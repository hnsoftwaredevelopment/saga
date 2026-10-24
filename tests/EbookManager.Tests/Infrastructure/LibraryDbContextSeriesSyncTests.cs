using EbookManager.Application.Books;
using EbookManager.Application.Series;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Series;
using EbookManager.Infrastructure.Files;
using EbookManager.Infrastructure.Metadata;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Series_sync_uses_committed_metadata_and_reports_file_warning(bool failWrite)
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var book = CreateBook("Book", ["Author"]);
        await books.AddAsync(book, CreateFile(book.Id), default);
        var repository = new EfSeriesRepository(factory, library.DirectoryPath);
        var sidecars = new SeriesSidecars(failWrite);
        var service = new SeriesRenameService(repository, new BookService(books,
            new ManagedLibraryFileStore(library.DirectoryPath), new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));
        var preview = await service.PreviewAsync("Series", default);
        var result = await service.RenameAsync(preview, "Renamed", default);
        result.Change.Status.Should().Be(SeriesRenameStatus.Renamed);
        result.Warnings.Count.Should().Be(failWrite ? 1 : 0);
        sidecars.Series.Should().Be("Renamed");
        (await books.GetAsync(book.Id, default))!.Metadata.Series.Should().Be("Renamed");
        (await books.GetAsync(book.Id, default))!.Metadata.SeriesNumber.Should().Be(book.Metadata.SeriesNumber);
    }

    [Fact]
    public async Task Series_sync_writes_real_metadata_json()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var book = CreateBook("Book", ["Author"]);
        await books.AddAsync(book, CreateFile(book.Id), default);
        var repository = new EfSeriesRepository(factory, library.DirectoryPath);
        var sidecars = new JsonMetadataSidecarStore();
        var service = new SeriesRenameService(repository, new BookService(books,
            new ManagedLibraryFileStore(library.DirectoryPath), new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));
        var result = await service.RenameAsync(await service.PreviewAsync("Series", default), "New", default);
        result.Warnings.Should().BeEmpty();
        var file = Directory.GetFiles(library.DirectoryPath, "metadata.json", SearchOption.AllDirectories).Single();
        (await sidecars.TryReadAsync(file, default))!.Series.Should().Be("New");
    }

    [Fact]
    public async Task Series_sync_finishes_all_books_if_request_is_cancelled_after_commit()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var first = CreateBook("First", ["Author"]);
        var second = CreateBook("Second", ["Author"]);
        await books.AddAsync(first, CreateFile(first.Id, Hash(1)), default);
        await books.AddAsync(second, CreateFile(second.Id, Hash(2)), default);
        using var cancellation = new CancellationTokenSource();
        var repository = new CancelAfterSeriesCommit(new EfSeriesRepository(factory, library.DirectoryPath), cancellation);
        var sidecars = new SeriesSidecars(false);
        var service = new SeriesRenameService(repository, new BookService(books, new ManagedLibraryFileStore(library.DirectoryPath),
            new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));
        var preview = await service.PreviewAsync("Series", cancellation.Token);
        var result = await service.RenameAsync(preview, "New", cancellation.Token);
        cancellation.IsCancellationRequested.Should().BeTrue();
        result.Change.Status.Should().Be(SeriesRenameStatus.Renamed);
        result.Change.Books.Should().HaveCount(2);
        result.Warnings.Should().BeEmpty();
        sidecars.Writes.Should().Be(2);
        sidecars.Series.Should().Be("New");
    }

    private sealed class CancelAfterSeriesCommit(ISeriesRepository inner, CancellationTokenSource cancellation) : ISeriesRepository
    {
        public Task<SeriesPage> SearchAsync(string query, int skip, int take, CancellationToken token) => inner.SearchAsync(query, skip, take, token);
        public Task<SeriesBookPage> ListBooksAsync(string name, int skip, int take, CancellationToken token) => inner.ListBooksAsync(name, skip, take, token);
        public Task<SeriesRenamePreview> PreviewRenameAsync(string name, CancellationToken token) => inner.PreviewRenameAsync(name, token);
        public async Task<SeriesRenameResult> RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token)
        {
            var result = await inner.RenameAsync(preview, name, token);
            cancellation.Cancel();
            return result;
        }
    }

    private sealed class SeriesSidecars(bool fail) : IMetadataSidecarStore
    {
        public string? Series { get; private set; }
        public int Writes { get; private set; }
        public Task<BookMetadata?> TryReadAsync(string path, CancellationToken token) => Task.FromResult<BookMetadata?>(null);
        public Task WriteAsync(string path, BookMetadata metadata, CancellationToken token)
        {
            token.Should().Be(CancellationToken.None);
            Writes++;
            Series = metadata.Series;
            if (fail) throw new IOException("Simulated write failure");
            return Task.CompletedTask;
        }
    }
}

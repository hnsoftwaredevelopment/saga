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

    private sealed class SeriesSidecars(bool fail) : IMetadataSidecarStore
    {
        public string? Series { get; private set; }
        public Task<BookMetadata?> TryReadAsync(string path, CancellationToken token) => Task.FromResult<BookMetadata?>(null);
        public Task WriteAsync(string path, BookMetadata metadata, CancellationToken token)
        {
            token.Should().Be(CancellationToken.None);
            Series = metadata.Series;
            if (fail) throw new IOException("Simulated write failure");
            return Task.CompletedTask;
        }
    }
}

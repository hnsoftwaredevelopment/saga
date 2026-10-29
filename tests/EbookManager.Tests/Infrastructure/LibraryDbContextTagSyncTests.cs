using EbookManager.Application.Books;
using EbookManager.Application.Tags;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Tags;
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
    public async Task Tag_sync_uses_committed_metadata_and_reports_file_warning(bool failWrite)
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var book = CreateBook("Book", ["Author"], ["Target"]);
        await books.AddAsync(book, CreateFile(book.Id), default);
        var repository = new EfTagRepository(factory, library.DirectoryPath);
        var sidecars = new TagSidecars(failWrite);
        var service = new TagRenameService(repository, new BookService(books,
            new ManagedLibraryFileStore(library.DirectoryPath), new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));
        var preview = await service.PreviewAsync((await repository.SearchAsync("Target", 0, 200, default)).Tags.Single().Id, default);
        var result = await service.RenameAsync(preview, "Renamed", default);
        result.Change.Status.Should().Be(TagRenameStatus.Renamed);
        result.Warnings.Count.Should().Be(failWrite ? 1 : 0);
        sidecars.Tags.Should().Equal("Renamed");
        (await books.GetAsync(book.Id, default))!.Metadata.Tags.Should().Equal("Renamed");
        (await books.GetAsync(book.Id, default))!.Metadata.SeriesNumber.Should().Be(book.Metadata.SeriesNumber);
    }

    [Fact]
    public async Task Tag_sync_writes_real_metadata_json()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var book = CreateBook("Book", ["Author"], ["Target"]);
        await books.AddAsync(book, CreateFile(book.Id), default);
        var repository = new EfTagRepository(factory, library.DirectoryPath);
        var sidecars = new JsonMetadataSidecarStore();
        var service = new TagRenameService(repository, new BookService(books,
            new ManagedLibraryFileStore(library.DirectoryPath), new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));
        var result = await service.RenameAsync(await service.PreviewAsync((await repository.SearchAsync("Target", 0, 200, default)).Tags.Single().Id, default), "New", default);
        result.Warnings.Should().BeEmpty();
        var file = Directory.GetFiles(library.DirectoryPath, "metadata.json", SearchOption.AllDirectories).Single();
        (await sidecars.TryReadAsync(file, default))!.Tags.Should().Equal("New");
    }

    [Fact]
    public async Task Tag_sync_finishes_all_books_if_request_is_cancelled_after_commit()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var first = CreateBook("First", ["Author"], ["Target"]);
        var second = CreateBook("Second", ["Author"], ["Target"]);
        await books.AddAsync(first, CreateFile(first.Id, Hash(1)), default);
        await books.AddAsync(second, CreateFile(second.Id, Hash(2)), default);
        using var cancellation = new CancellationTokenSource();
        var repository = new CancelAfterTagCommit(new EfTagRepository(factory, library.DirectoryPath), cancellation);
        var sidecars = new TagSidecars(false);
        var service = new TagRenameService(repository, new BookService(books, new ManagedLibraryFileStore(library.DirectoryPath),
            new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));
        var preview = await service.PreviewAsync((await repository.SearchAsync("Target", 0, 200, default)).Tags.Single().Id, cancellation.Token);
        var result = await service.RenameAsync(preview, "New", cancellation.Token);
        cancellation.IsCancellationRequested.Should().BeTrue();
        result.Change.Status.Should().Be(TagRenameStatus.Renamed);
        result.Change.Books.Should().HaveCount(2);
        result.Warnings.Should().BeEmpty();
        sidecars.Writes.Should().Be(2);
        sidecars.Tags.Should().Equal("New");
    }

    private sealed class CancelAfterTagCommit(ITagRepository inner, CancellationTokenSource cancellation) : ITagRepository
    {
        public Task<TagPage> SearchAsync(string query, int skip, int take, CancellationToken token) => inner.SearchAsync(query, skip, take, token);
        public Task<TagBookPage> ListBooksAsync(Guid tagId, int skip, int take, CancellationToken token) => inner.ListBooksAsync(tagId, skip, take, token);
        public Task<TagRenamePreview> PreviewRenameAsync(Guid tagId, CancellationToken token) => inner.PreviewRenameAsync(tagId, token);
        public async Task<TagRenameResult> RenameAsync(TagRenamePreview preview, string name, CancellationToken token)
        {
            var result = await inner.RenameAsync(preview, name, token);
            cancellation.Cancel();
            return result;
        }
    }

    private sealed class TagSidecars(bool fail) : IMetadataSidecarStore
    {
        public IReadOnlyList<string>? Tags { get; private set; }
        public int Writes { get; private set; }
        public Task<BookMetadata?> TryReadAsync(string path, CancellationToken token) => Task.FromResult<BookMetadata?>(null);
        public Task WriteAsync(string path, BookMetadata metadata, CancellationToken token)
        {
            token.Should().Be(CancellationToken.None);
            Writes++;
            Tags = metadata.Tags;
            if (fail) throw new IOException("Simulated write failure");
            return Task.CompletedTask;
        }
    }
}

using EbookManager.Application.Authors;
using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Authors;
using EbookManager.Infrastructure.Files;
using EbookManager.Infrastructure.Metadata;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Fact]
    public async Task AuthorMerge_service_writes_current_authors_to_real_metadata_json()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var source = CreateBook("Source book", ["Source", "Coauthor"]);
        var target = CreateBook("Target book", ["Target"]);
        await books.AddAsync(source, CreateFile(source.Id, Hash(1)), default);
        await books.AddAsync(target, CreateFile(target.Id, Hash(2)), default);
        var repository = new EfAuthorProfileRepository(factory, library.DirectoryPath);
        var selected = (await repository.SearchAsync("", 0, 200, default)).Authors.Where(author => author.Name != "Coauthor").ToArray();
        var targetId = selected.Single(author => author.Name == "Target").Id;
        var sidecars = new JsonMetadataSidecarStore();
        var service = new AuthorMergeService(repository, repository, new ManagedAuthorPhotoStore(library.DirectoryPath),
            new BookService(books, new ManagedLibraryFileStore(library.DirectoryPath), new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));
        var result = await service.MergeAsync((await service.PreviewAsync(selected.Select(author => author.Id).ToArray(), default))!,
            new(targetId, null, null, null), default);
        result.Change.Status.Should().Be(AuthorMergeStatus.Merged);
        result.Warnings.Should().BeEmpty();
        var path = Directory.GetFiles(library.DirectoryPath, "metadata.json", SearchOption.AllDirectories).Single();
        (await sidecars.TryReadAsync(path, default))!.Authors.Should().Equal("Target", "Coauthor");
    }

    [Theory]
    [InlineData(false, false, "none")]
    [InlineData(true, false, "none")]
    [InlineData(false, true, "none")]
    [InlineData(false, false, "read")]
    [InlineData(false, false, "save")]
    [InlineData(false, false, "cleanup")]
    public async Task AuthorMerge_service_copies_photo_to_target_and_finishes_files_after_commit(bool failFiles, bool reject, string photoFault)
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var book = CreateBook("Book", ["Source"]);
        var existing = CreateBook(reject ? "Book" : "Existing", ["Target"]);
        await books.AddAsync(book, CreateFile(book.Id, Hash(1)), default);
        await books.AddAsync(existing, CreateFile(existing.Id, Hash(2)), default);
        var photos = new ManagedAuthorPhotoStore(library.DirectoryPath);
        var repository = new EfAuthorProfileRepository(factory, library.DirectoryPath);
        await using var context = factory.Create(library.DirectoryPath);
        var source = await context.Authors.SingleAsync(author => author.Name == "Source");
        var target = await context.Authors.SingleAsync(author => author.Name == "Target");
        source.PhotoRelativePath = await photos.SaveJpegAsync(source.Id, [0xff, 0xd8, 1, 2], default);
        target.PhotoRelativePath = await photos.SaveJpegAsync(target.Id, [0xff, 0xd8, 3, 4], default);
        await context.SaveChangesAsync();
        using var cancellation = new CancellationTokenSource();
        var sidecars = new TagSidecars(failFiles);
        var service = new AuthorMergeService(new CancelAfterAuthorMerge(repository, cancellation), repository, new AuthorMergePhotoFaults(photos, photoFault),
            new BookService(books, new ManagedLibraryFileStore(library.DirectoryPath), new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));
        var preview = (await service.PreviewAsync([source.Id, target.Id], default))!;
        if (photoFault is "read" or "save")
        {
            await FluentActions.Awaiting(() => service.MergeAsync(preview, new(target.Id, null, null, source.Id), cancellation.Token)).Should().ThrowAsync<IOException>();
            (await books.GetAsync(book.Id, default))!.Metadata.Authors.Should().Equal("Source");
            Directory.GetFiles(library.DirectoryPath, "*.jpg", SearchOption.AllDirectories).Should().HaveCount(2);
            sidecars.Writes.Should().Be(0);
            return;
        }
        var result = await service.MergeAsync(preview, new(target.Id, null, null, source.Id), cancellation.Token);
        if (reject)
        {
            result.Change.Status.Should().Be(AuthorMergeStatus.DuplicateBook);
            sidecars.Writes.Should().Be(0);
            (await photos.ReadAsync(source.Id, source.PhotoRelativePath, default)).Should().NotBeNull();
            Directory.GetFiles(library.DirectoryPath, "*.jpg", SearchOption.AllDirectories).Should().HaveCount(2);
            return;
        }
        result.Change.Status.Should().Be(AuthorMergeStatus.Merged);
        result.Warnings.Count.Should().Be(failFiles ? 1 : 0);
        result.CleanupWarning.Should().Be(photoFault == "cleanup");
        cancellation.IsCancellationRequested.Should().BeTrue();
        sidecars.Writes.Should().Be(1);
        (await books.GetAsync(book.Id, default))!.Metadata.Authors.Should().Equal("Target");
        (await photos.ReadAsync(target.Id, result.Change.Profile!.PhotoRelativePath!, default)).Should().Equal([0xff, 0xd8, 1, 2]);
        Directory.GetFiles(library.DirectoryPath, "*.jpg", SearchOption.AllDirectories).Should().HaveCount(photoFault == "cleanup" ? 3 : 1);
    }

    private sealed class AuthorMergePhotoFaults(IAuthorPhotoStore inner, string fault) : IAuthorPhotoStore
    {
        public Task<byte[]?> ReadAsync(Guid id, string path, CancellationToken token) => fault == "read" ? throw new IOException("Simulated photo read failure") : inner.ReadAsync(id, path, token);
        public Task<string> SaveJpegAsync(Guid id, byte[] bytes, CancellationToken token) => fault == "save" ? throw new IOException("Simulated photo save failure") : inner.SaveJpegAsync(id, bytes, token);
        public Task DeleteAsync(Guid id, string path, CancellationToken token) => fault == "cleanup" ? throw new IOException("Simulated photo cleanup failure") : inner.DeleteAsync(id, path, token);
    }

    private sealed class CancelAfterAuthorMerge(IAuthorMergeRepository inner, CancellationTokenSource cancellation) : IAuthorMergeRepository
    {
        public Task<AuthorMergePreview?> PreviewMergeAsync(IReadOnlyCollection<Guid> ids, CancellationToken token) => inner.PreviewMergeAsync(ids, token);
        public async Task<AuthorMergeResult> MergeAsync(AuthorMergePreview preview, AuthorMergeSelection selection, string? path, CancellationToken token)
        {
            var result = await inner.MergeAsync(preview, selection, path, token);
            cancellation.Cancel();
            return result;
        }
    }
}

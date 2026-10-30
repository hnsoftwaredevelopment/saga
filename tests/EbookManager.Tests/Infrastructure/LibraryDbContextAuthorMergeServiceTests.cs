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
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task AuthorMerge_service_copies_photo_to_target_and_finishes_files_after_commit(bool failFiles, bool reject)
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
        var service = new AuthorMergeService(new CancelAfterAuthorMerge(repository, cancellation), repository, photos,
            new BookService(books, new ManagedLibraryFileStore(library.DirectoryPath), new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));
        var preview = (await service.PreviewAsync([source.Id, target.Id], default))!;
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
        result.CleanupWarning.Should().BeFalse();
        cancellation.IsCancellationRequested.Should().BeTrue();
        sidecars.Writes.Should().Be(1);
        (await books.GetAsync(book.Id, default))!.Metadata.Authors.Should().Equal("Target");
        (await photos.ReadAsync(target.Id, result.Change.Profile!.PhotoRelativePath!, default)).Should().Equal([0xff, 0xd8, 1, 2]);
        Directory.GetFiles(library.DirectoryPath, "*.jpg", SearchOption.AllDirectories).Should().HaveCount(1);
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

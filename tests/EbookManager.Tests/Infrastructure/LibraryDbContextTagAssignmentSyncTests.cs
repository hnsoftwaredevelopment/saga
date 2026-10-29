using EbookManager.Application.Books;
using EbookManager.Application.Tags;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Tags;
using EbookManager.Infrastructure.Files;
using EbookManager.Infrastructure.Metadata;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Theory]
    [InlineData(TagAssignmentOperation.Replace, false)]
    [InlineData(TagAssignmentOperation.Replace, true)]
    [InlineData(TagAssignmentOperation.Remove, false)]
    [InlineData(TagAssignmentOperation.Remove, true)]
    public async Task Tag_assignment_sync_reports_warnings_after_storing_only_selected_metadata(TagAssignmentOperation operation, bool fail)
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var book = CreateBook("Book", ["Author"], ["Source", "Keep"]);
        await books.AddAsync(book, CreateFile(book.Id), default);
        var repository = new EfTagRepository(factory, library.DirectoryPath);
        var id = (await repository.SearchAsync("Source", 0, 200, default)).Tags.Single().Id;
        var sidecars = new TagSidecars(fail);
        var service = new TagAssignmentService(repository, new BookService(books, new ManagedLibraryFileStore(library.DirectoryPath),
            new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));
        var result = await service.ApplyAsync(await service.PreviewAsync(id, [book.Id], default), new(operation, "New"), default);
        result.Change.Status.Should().Be(TagAssignmentStatus.Applied);
        result.Warnings.Count.Should().Be(fail ? 1 : 0);
        sidecars.Writes.Should().Be(1);
        sidecars.Tags.Should().Equal(operation == TagAssignmentOperation.Remove ? ["Keep"] : ["New", "Keep"]);
        (await books.GetAsync(book.Id, default))!.Metadata.Tags.Should().Equal(sidecars.Tags!);
    }

    [Fact]
    public async Task Tag_assignment_sync_finishes_after_commit_and_writes_real_sidecars()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var book = CreateBook("Book", ["Author"], ["Source", "Keep"]);
        await books.AddAsync(book, CreateFile(book.Id), default);
        var repository = new EfTagRepository(factory, library.DirectoryPath);
        var id = (await repository.SearchAsync("Source", 0, 200, default)).Tags.Single().Id;
        using var cancellation = new CancellationTokenSource();
        var sidecars = new JsonMetadataSidecarStore();
        var service = new TagAssignmentService(new CancelTagAssignments(repository, cancellation), new BookService(books,
            new ManagedLibraryFileStore(library.DirectoryPath), new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));
        var preview = await service.PreviewAsync(id, [book.Id], cancellation.Token);
        var result = await service.ApplyAsync(preview, new(TagAssignmentOperation.Remove), cancellation.Token);
        cancellation.IsCancellationRequested.Should().BeTrue();
        result.Warnings.Should().BeEmpty();
        var file = Directory.GetFiles(library.DirectoryPath, "metadata.json", SearchOption.AllDirectories).Single();
        (await sidecars.TryReadAsync(file, default))!.Tags.Should().Equal("Keep");
    }

    private sealed class CancelTagAssignments(ITagAssignmentRepository inner, CancellationTokenSource cancellation) : ITagAssignmentRepository
    {
        public Task<TagAssignmentPreview> PreviewAssignmentsAsync(Guid tagId, IReadOnlyCollection<Guid> ids, CancellationToken token) => inner.PreviewAssignmentsAsync(tagId, ids, token);
        public async Task<TagAssignmentResult> ApplyAssignmentsAsync(TagAssignmentPreview preview, TagAssignmentRequest request, CancellationToken token)
        {
            var result = await inner.ApplyAssignmentsAsync(preview, request, token);
            cancellation.Cancel();
            return result;
        }
    }
}

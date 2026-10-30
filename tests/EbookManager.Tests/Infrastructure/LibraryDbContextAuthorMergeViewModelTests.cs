using EbookManager.Application.Authors;
using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Authors;
using EbookManager.Infrastructure.Files;
using EbookManager.Infrastructure.Metadata;
using EbookManager.Infrastructure.Persistence.Repositories;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task AuthorMerge_viewmodel_protects_drafts_refreshes_survivor_and_preserves_committed_warnings(bool failRefresh, bool candidateFlow)
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var book = CreateBook("Source book", ["Source"]);
        var existing = CreateBook("Target book", ["Target"]);
        await books.AddAsync(book, CreateFile(book.Id, Hash(1)), default);
        await books.AddAsync(existing, CreateFile(existing.Id, Hash(2)), default);
        var repository = new EfAuthorProfileRepository(factory, library.DirectoryPath);
        var reloads = new AuthorMergeReloads(repository);
        var photos = new ManagedAuthorPhotoStore(library.DirectoryPath);
        var service = new AuthorMergeService(repository, repository, photos, new BookService(books,
            new ManagedLibraryFileStore(library.DirectoryPath), new MetadataAdapterResolver([new FallbackMetadataAdapter()]), new TagSidecars(failRefresh)));
        using var vm = new AuthorManagementViewModel(reloads, photos, key => key, mergeService: service);
        await vm.SearchAsync("");
        await vm.SelectAsync(vm.Authors.Single(author => author.Name == "Source"));
        vm.Biography = "Unsaved draft";
        vm.SetMergeAuthors(vm.Authors);
        vm.CanMerge.Should().BeFalse();
        vm.DiscardChanges();
        vm.CanMerge.Should().BeTrue();
        vm.CanEdit.Should().BeFalse();
        var target = vm.Authors.Single(author => author.Name == "Target");
        var source = vm.Authors.Single(author => author.Name == "Source");
        var targetId = target.Id;
        AuthorMergePreview preview;
        if (candidateFlow)
        {
            await vm.SearchAsync("Target");
            vm.Authors.Should().ContainSingle();
            vm.CanMerge.Should().BeFalse();
            vm.CanFindDuplicates.Should().BeTrue();
            preview = (await vm.PreviewCandidateAsync(new(source, target, AuthorDuplicateReason.MatchingInitials)))!;
            (await vm.MergeCandidateAsync(preview with { Authors = preview.Authors.ToArray() }, new(targetId, null, null, null))).Should().BeFalse();
        }
        else preview = (await vm.PreviewMergeAsync())!;
        reloads.FailLoads = failRefresh;
        var merged = candidateFlow ? await vm.MergeCandidateAsync(preview, new(targetId, null, null, null)) : await vm.MergeAsync(preview, new(targetId, null, null, null));
        merged.Should().BeTrue();
        vm.ChangedBookIds.Should().Equal(book.Id);
        vm.SelectedAuthorId.Should().Be(targetId);
        vm.Name.Should().Be("Target");
        vm.IsBusy.Should().BeFalse();
        vm.Authors.Single().BookCount.Should().Be(2);
        if (failRefresh)
        {
            vm.StatusMessage.Should().Be("AuthorRenameRefreshFailed");
            vm.RenameWarnings.Should().ContainSingle(warning => warning.Id == book.Id);
            vm.IsError.Should().BeTrue();
        }
        else
        {
            vm.Books.Should().HaveCount(2);
            vm.TotalBooks.Should().Be(2);
            vm.MergeAuthorIds.Should().Equal(targetId);
            vm.IsError.Should().BeFalse();
            await vm.SearchAsync("Target");
            vm.MergeSelectionCount.Should().Be(0);
        }
    }

    private sealed class AuthorMergeReloads(IAuthorProfileRepository inner) : IAuthorProfileRepository
    {
        public bool FailLoads { get; set; }
        public Task<AuthorPage> SearchAsync(string query, int skip, int take, CancellationToken token) => inner.SearchAsync(query, skip, take, token);
        public Task<AuthorProfile?> GetAsync(Guid id, CancellationToken token) => inner.GetAsync(id, token);
        public Task<IReadOnlyList<AuthorBook>> ListBooksAsync(Guid id, int skip, int take, CancellationToken token) =>
            FailLoads ? throw new IOException("Simulated reload failure") : inner.ListBooksAsync(id, skip, take, token);
        public Task<AuthorProfileSaveStatus> SaveAsync(AuthorProfile original, AuthorProfile updated, CancellationToken token) => inner.SaveAsync(original, updated, token);
    }
}

using EbookManager.Application.Authors;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Authors;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class AuthorManagementViewModelTests
{
    [Fact]
    public async Task Failed_selection_never_leaves_another_authors_profile_editable()
    {
        var repository = new Repository();
        using var vm = new AuthorManagementViewModel(repository, new Photos(), key => key);
        await vm.SelectAsync(new(repository.Profile.Id, "Author", 0));
        repository.FailRead = true;

        await vm.SelectAsync(new(Guid.NewGuid(), "Unavailable author", 0));

        vm.HasProfile.Should().BeFalse();
        vm.CanEdit.Should().BeFalse();
        vm.StatusMessage.Should().Be("AuthorProfileLoadFailed");
    }

    [Fact]
    public async Task Editing_is_a_draft_until_saved_and_discard_restores_original()
    {
        var repository = new Repository();
        var photos = new Photos();
        using var vm = new AuthorManagementViewModel(repository, photos, key => key);
        await vm.SearchAsync("");
        await vm.SelectAsync(vm.Authors.Single());
        vm.Biography = "Draft biography";
        vm.HasChanges.Should().BeTrue();
        repository.Profile.Biography.Should().BeNull();
        vm.DiscardChanges();
        vm.HasChanges.Should().BeFalse();
        vm.Biography.Should().BeEmpty();
        vm.Website = "file:///c:/test";
        (await vm.SaveAsync()).Should().BeFalse();
        vm.StatusMessage.Should().Be("AuthorProfileInvalid");
        vm.Website = "https://example.org";
        (await vm.SaveAsync()).Should().BeTrue();
        repository.Profile.WebsiteUrl.Should().Be("https://example.org");
        vm.HasChanges.Should().BeFalse();
    }

    [Fact]
    public async Task Late_selection_does_not_replace_a_newer_author_or_closed_window()
    {
        var repository = new Repository();
        var deferred = new TaskCompletionSource<AuthorProfile?>(TaskCreationOptions.RunContinuationsAsynchronously);
        repository.DelayedId = Guid.NewGuid();
        repository.Delayed = deferred.Task;
        var vm = new AuthorManagementViewModel(repository, new Photos(), key => key);
        var first = vm.SelectAsync(new(repository.DelayedId, "Old selection", 0));
        await vm.SelectAsync(new(repository.Profile.Id, repository.Profile.Name, 0));
        deferred.SetResult(new(repository.DelayedId, "Late", null, null, null));
        await first;
        vm.Name.Should().Be("Author");
        vm.Dispose();
    }

    private sealed class Repository : IAuthorProfileRepository
    {
        public AuthorProfile Profile = new(Guid.NewGuid(), "Author", null, null, null);
        public Guid DelayedId;
        public Task<AuthorProfile?>? Delayed;
        public bool FailRead;
        public Task<AuthorPage> SearchAsync(string query, int skip, int take, CancellationToken token) =>
            Task.FromResult(new AuthorPage([new(Profile.Id, Profile.Name, 0)], 1));
        public Task<AuthorProfile?> GetAsync(Guid id, CancellationToken token) => FailRead
            ? Task.FromException<AuthorProfile?>(new IOException("Cannot read"))
            : id == DelayedId ? Delayed! : Task.FromResult<AuthorProfile?>(Profile);
        public Task<IReadOnlyList<AuthorBook>> ListBooksAsync(Guid id, int skip, int take, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<AuthorBook>>([]);
        public Task<AuthorProfileSaveStatus> SaveAsync(AuthorProfile original, AuthorProfile updated, CancellationToken token)
        {
            Profile = updated;
            return Task.FromResult(AuthorProfileSaveStatus.Saved);
        }
    }

    private sealed class Photos : IAuthorPhotoStore
    {
        public Task<string> SaveJpegAsync(Guid id, byte[] jpeg, CancellationToken token) => Task.FromResult("photo");
        public Task<byte[]?> ReadAsync(Guid id, string path, CancellationToken token) => Task.FromResult<byte[]?>(null);
        public Task DeleteAsync(Guid id, string path, CancellationToken token) => Task.CompletedTask;
    }
}

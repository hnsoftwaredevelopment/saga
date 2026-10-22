using EbookManager.Application.Authors;
using EbookManager.Domain.Authors;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed partial class AuthorManagementViewModelTests
{
    [Fact]
    public async Task Rename_keeps_book_pagination_in_the_existing_title_order()
    {
        var repository = new Repository
        {
            AuthorBooks = Enumerable.Range(0, 205).Select(index => new AuthorBook(Guid.NewGuid(), $"Book {index:D3}", null)).ToArray()
        };
        var rename = new RenameService(repository) { ResultBooks = repository.AuthorBooks.Reverse().ToArray() };
        using var vm = new AuthorManagementViewModel(repository, new Photos(), key => key, rename);
        await vm.SelectAsync(new(repository.Profile.Id, "Author", 205));
        var preview = (await vm.PreviewRenameAsync())!;
        await vm.RenameAsync(preview, "Renamed");

        await vm.LoadMoreBooksAsync();

        vm.Books.Select(book => book.Id).Should().OnlyHaveUniqueItems();
        vm.Books.Should().HaveCount(205);
        vm.Books.Select(book => book.Title).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Selecting_an_author_clears_file_warnings_from_the_previous_rename()
    {
        var repository = new Repository();
        var rename = new RenameService(repository) { WithWarning = true };
        using var vm = new AuthorManagementViewModel(repository, new Photos(), key => key, rename);
        await vm.SelectAsync(new(repository.Profile.Id, "Author", 1));
        var preview = (await vm.PreviewRenameAsync())!;
        await vm.RenameAsync(preview, "Renamed");
        vm.RenameWarnings.Should().ContainSingle();

        await vm.SelectAsync(new(repository.Profile.Id, "Renamed", 1));

        vm.RenameWarnings.Should().BeEmpty();
        vm.StatusMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task Rename_keeps_selected_profile_and_tracks_books_for_main_window_refresh()
    {
        var repository = new Repository();
        var rename = new RenameService(repository);
        using var vm = new AuthorManagementViewModel(repository, new Photos(), key => key, rename);
        await vm.SelectAsync(new(repository.Profile.Id, "Author", 1));
        var preview = (await vm.PreviewRenameAsync())!;

        (await vm.RenameAsync(preview, "Renamed")).Should().BeTrue();

        vm.Name.Should().Be("Renamed");
        vm.SelectedAuthorId.Should().Be(preview.Profile.Id);
        vm.ChangedBookIds.Should().Equal(rename.Book.Id);
        vm.HasChanges.Should().BeFalse();
        vm.StatusMessage.Should().Be("AuthorRenameSaved");
    }

    [Fact]
    public async Task Existing_name_and_pending_profile_edits_never_change_author_or_books()
    {
        var repository = new Repository();
        var rename = new RenameService(repository) { Status = AuthorRenameStatus.NameExists };
        using var vm = new AuthorManagementViewModel(repository, new Photos(), key => key, rename);
        await vm.SelectAsync(new(repository.Profile.Id, "Author", 1));
        vm.Biography = "Unsaved";
        (await vm.PreviewRenameAsync()).Should().BeNull();
        vm.DiscardChanges();
        var preview = (await vm.PreviewRenameAsync())!;
        (await vm.RenameAsync(preview, "Already exists")).Should().BeFalse();
        vm.Name.Should().Be("Author");
        vm.ChangedBookIds.Should().BeEmpty();
        vm.StatusMessage.Should().Be("AuthorRenameNameExists");
    }

    [Fact]
    public async Task A_second_rename_is_blocked_while_the_original_is_saving()
    {
        var repository = new Repository();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var rename = new RenameService(repository) { Gate = gate.Task };
        using var vm = new AuthorManagementViewModel(repository, new Photos(), key => key, rename);
        await vm.SelectAsync(new(repository.Profile.Id, "Author", 1));
        var preview = (await vm.PreviewRenameAsync())!;
        var first = vm.RenameAsync(preview, "Renamed");
        vm.CanRename.Should().BeFalse();
        (await vm.RenameAsync(preview, "Another rename")).Should().BeFalse();
        gate.SetResult();
        (await first).Should().BeTrue();
    }

    private sealed class RenameService(Repository repository) : IAuthorRenameService
    {
        public AuthorRenameStatus Status = AuthorRenameStatus.Renamed;
        public AuthorBook Book = new(Guid.NewGuid(), "Title", null);
        public Task? Gate;
        public bool WithWarning;
        public IReadOnlyList<AuthorBook>? ResultBooks;
        public Task<AuthorRenamePreview?> PreviewAsync(Guid id, CancellationToken token) =>
            Task.FromResult<AuthorRenamePreview?>(new(repository.Profile, [Book.Id]));
        public async Task<AuthorRenameCompletion> RenameAsync(AuthorRenamePreview preview, string name, CancellationToken token)
        {
            if (Gate is not null) await Gate;
            if (Status != AuthorRenameStatus.Renamed) return new(new(Status, null, []), []);
            repository.Profile = repository.Profile with { Name = name };
            return new(new(Status, repository.Profile, ResultBooks ?? [Book]), WithWarning ? [Book] : []);
        }
    }
}

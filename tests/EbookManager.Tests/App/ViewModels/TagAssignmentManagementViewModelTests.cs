using EbookManager.Application.Tags;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Tags;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class TagAssignmentManagementViewModelTests
{
    [Fact]
    public async Task Selected_replacement_refreshes_source_books_and_both_index_counts()
    {
        var state = new AssignmentState();
        using var vm = state.CreateViewModel();
        await vm.SearchAsync(""); await vm.SelectAsync(state.Source);
        vm.SetSelectedBooks([vm.Books.First()]);
        var changed = vm.SelectedBookIds.Single();
        var preview = (await vm.PreviewAssignmentAsync())!;
        (await vm.ApplyAssignmentAsync(preview, new(TagAssignmentOperation.Replace, "Target"))).Should().BeTrue();
        vm.Books.Should().ContainSingle().Which.Id.Should().NotBe(changed);
        vm.TotalBooks.Should().Be(1);
        vm.Tags.Single(tag => tag.Id == state.Source.Id).BookCount.Should().Be(1);
        vm.Tags.Single(tag => tag.Name == "Target").BookCount.Should().Be(2);
        vm.SelectedBookCount.Should().Be(0);
        vm.ChangedBookIds.Should().Equal(changed);
        vm.StatusMessage.Should().Be("TagAssignmentSaved");
    }

    [Fact]
    public async Task Removing_the_last_source_members_clears_the_pane_and_keeps_books_and_target()
    {
        var state = new AssignmentState();
        using var vm = state.CreateViewModel();
        await vm.SearchAsync(""); await vm.SelectAsync(state.Source);
        vm.SetSelectedBooks(vm.Books);
        var preview = (await vm.PreviewAssignmentAsync())!;
        (await vm.ApplyAssignmentAsync(preview, new(TagAssignmentOperation.Remove))).Should().BeTrue();
        vm.TagId.Should().BeNull();
        vm.Name.Should().BeEmpty();
        vm.Books.Should().BeEmpty();
        vm.TotalBooks.Should().Be(0);
        vm.Tags.Should().ContainSingle().Which.Name.Should().Be("Target");
        state.Memberships.Should().HaveCount(3);
        vm.SelectedBookCount.Should().Be(0);
    }

    [Fact]
    public async Task Preview_is_nonmutating_and_a_newer_selection_invalidates_it()
    {
        var state = new AssignmentState();
        using var vm = state.CreateViewModel();
        await vm.SelectAsync(state.Source);
        vm.SetSelectedBooks([vm.Books.First()]);
        var preview = (await vm.PreviewAssignmentAsync())!;
        state.Applications.Should().Be(0);
        vm.SetSelectedBooks([vm.Books.Last()]);
        (await vm.ApplyAssignmentAsync(preview, new(TagAssignmentOperation.Remove))).Should().BeFalse();
        state.Applications.Should().Be(0);
        vm.SelectedBookCount.Should().Be(1);
        vm.ChangedBookIds.Should().BeEmpty();
    }

    [Fact]
    public async Task More_books_preserves_selection_and_a_new_tag_clears_it()
    {
        var state = new AssignmentState();
        for (var index = 0; index < 203; index++) state.Memberships.Add(Guid.NewGuid(), [state.Source.Id]);
        using var vm = state.CreateViewModel();
        await vm.SelectAsync(state.Source);
        vm.SetSelectedBooks([vm.Books[10]]);
        var selected = vm.SelectedBookIds.ToArray();
        await vm.LoadMoreBooksAsync();
        vm.Books.Should().HaveCount(205);
        vm.SelectedBookIds.Should().Equal(selected);
        await vm.SelectAsync((await state.SearchAsync("Target", 0, 200, default)).Tags.Single());
        vm.SelectedBookCount.Should().Be(0);
    }

    [Fact]
    public async Task Failure_to_refresh_after_assignment_reports_saved_and_retains_changed_ids()
    {
        var state = new AssignmentState();
        using var vm = state.CreateViewModel();
        await vm.SelectAsync(state.Source); vm.SetSelectedBooks([vm.Books.First()]);
        var preview = (await vm.PreviewAssignmentAsync())!;
        state.FailRefresh = true;
        (await vm.ApplyAssignmentAsync(preview, new(TagAssignmentOperation.Remove))).Should().BeTrue();
        vm.StatusMessage.Should().Be("TagAssignmentRefreshFailed");
        vm.ChangedBookIds.Should().ContainSingle();
        vm.IsBusy.Should().BeFalse();
        vm.SelectedBookCount.Should().Be(0);
    }

    private sealed class AssignmentState : ITagRepository, ITagRenameService, ITagAssignmentService
    {
        public TagSummary Source { get; } = new(Guid.NewGuid(), "Source", 2);
        private readonly Guid target = Guid.NewGuid();
        public Dictionary<Guid, HashSet<Guid>> Memberships { get; } = [];
        public int Applications { get; private set; }
        public bool FailRefresh { get; set; }
        public AssignmentState()
        {
            Memberships.Add(Guid.NewGuid(), [Source.Id, target]);
            Memberships.Add(Guid.NewGuid(), [Source.Id]);
            Memberships.Add(Guid.NewGuid(), [target]);
        }
        public TagManagementViewModel CreateViewModel() => new(this, this, key => key, this);
        public Task<TagPage> SearchAsync(string query, int skip, int take, CancellationToken token)
        {
            if (FailRefresh && Applications > 0) throw new IOException("Sensitive path");
            var tags = new[] { Source with { BookCount = Memberships.Count(row => row.Value.Contains(Source.Id)) },
                new TagSummary(target, "Target", Memberships.Count(row => row.Value.Contains(target))) }
                .Where(tag => tag.BookCount > 0 && tag.Name.Contains(query)).ToArray();
            return Task.FromResult(new TagPage(tags.Skip(skip).Take(take).ToArray(), tags.Length));
        }
        public Task<TagBookPage> ListBooksAsync(Guid id, int skip, int take, CancellationToken token)
        {
            var books = Memberships.Where(row => row.Value.Contains(id)).Select(row => new TagBook(row.Key, row.Key.ToString(), "Author")).ToArray();
            return Task.FromResult(new TagBookPage(books.Skip(skip).Take(take).ToArray(), books.Length));
        }
        public Task<TagAssignmentPreview> PreviewAsync(Guid id, IReadOnlyCollection<Guid> ids, CancellationToken token) =>
            Task.FromResult(new TagAssignmentPreview(id, "Source", ids.Select(book => new TagSourceBook(book, book.ToString())).ToArray()));
        public Task<TagAssignmentCompletion> ApplyAsync(TagAssignmentPreview preview, TagAssignmentRequest request, CancellationToken token)
        {
            Applications++;
            foreach (var book in preview.Books)
            {
                Memberships[book.Id].Remove(preview.TagId);
                if (request.Operation == TagAssignmentOperation.Replace) Memberships[book.Id].Add(target);
            }
            return Task.FromResult(new TagAssignmentCompletion(new(TagAssignmentStatus.Applied, preview.TagId,
                request.Operation == TagAssignmentOperation.Replace ? target : null, request.TargetName, preview.Books), []));
        }
        public Task<TagRenamePreview> PreviewRenameAsync(Guid id, CancellationToken token) => throw new NotSupportedException();
        public Task<TagRenamePreview> PreviewAsync(Guid id, CancellationToken token) => throw new NotSupportedException();
        public Task<TagRenameResult> RenameAsync(TagRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
        Task<TagRenameCompletion> ITagRenameService.RenameAsync(TagRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
    }
}

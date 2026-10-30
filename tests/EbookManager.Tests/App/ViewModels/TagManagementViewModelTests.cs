using EbookManager.Application.Tags;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Tags;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class TagManagementViewModelTests
{
    private static readonly Guid TagId = Guid.NewGuid();

    [Fact]
    public async Task Failed_book_reload_after_committed_merge_preserves_file_warnings_and_changed_ids()
    {
        var repository = new TagFake();
        using var vm = new TagManagementViewModel(repository, repository, key => key);
        await vm.SelectAsync(new(TagId, "Old", 2));
        var preview = (await vm.PreviewRenameAsync())!;
        var targetId = Guid.NewGuid();
        TagSourceBook[] warnings = [preview.Books[0]];
        repository.Rename = (_, _) => Task.FromResult(new TagRenameCompletion(
            new(TagRenameStatus.Renamed, targetId, "Roman", preview.Books), warnings));
        repository.List = (_, _, _) => throw new IOException("Sensitive path");

        (await vm.RenameAsync(preview, "Roman")).Should().BeTrue();

        vm.Warnings.Should().Equal(warnings);
        vm.ChangedBookIds.Should().BeEquivalentTo(preview.Books.Select(book => book.Id));
        vm.TagId.Should().Be(targetId);
        vm.Name.Should().Be("Roman");
        vm.StatusMessage.Should().Be("TagRenameRefreshFailed");
        vm.IsError.Should().BeTrue();
        vm.IsBusy.Should().BeFalse();
        vm.Books.Should().BeEmpty();
    }

    [Fact]
    public async Task Global_merge_selects_surviving_tag_and_reloads_its_books_without_requiring_book_selection()
    {
        var targetId = Guid.NewGuid();
        var repository = new TagFake();
        using var vm = new TagManagementViewModel(repository, repository, key => key);
        await vm.SelectAsync(new(TagId, "Old", 2));
        var preview = (await vm.PreviewRenameAsync())!;
        var all = preview.Books.Select(book => new TagBook(book.Id, book.Title, "Author"))
            .Append(new(Guid.NewGuid(), "Previously tagged", "Author")).ToArray();
        repository.Rename = (_, _) => Task.FromResult(new TagRenameCompletion(new(TagRenameStatus.Renamed, targetId, "Roman", preview.Books), []));
        repository.Search = _ => Task.FromResult(new TagPage([new(targetId, "Roman", 3)], 1));
        repository.List = (id, skip, take) => Task.FromResult(new TagBookPage(id == targetId ? all.Skip(skip).Take(take).ToArray() : [], id == targetId ? 3 : 0));
        vm.SelectedBookCount.Should().Be(0);
        (await vm.RenameAsync(preview, " roman ")).Should().BeTrue();
        vm.TagId.Should().Be(targetId);
        vm.Name.Should().Be("Roman");
        vm.Query.Should().Be("Roman");
        vm.Books.Should().Equal(all);
        vm.TotalBooks.Should().Be(3);
        vm.Tags.Single().BookCount.Should().Be(3);
        vm.ChangedBookIds.Should().BeEquivalentTo(preview.Books.Select(book => book.Id));
        vm.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Rename_keeps_ordered_books_selects_new_query_and_reports_committed_ids_and_warnings()
    {
        var repository = new TagFake();
        using var vm = new TagManagementViewModel(repository, repository, key => key);
        await vm.SearchAsync("Old");
        await vm.SelectAsync(new(TagId, "Old", 2));
        var sorted = vm.Books.ToArray();
        var preview = (await vm.PreviewRenameAsync())!;
        await vm.RenameAsync(preview, "ZZZ new");
        vm.Query.Should().Be("ZZZ new");
        vm.Name.Should().Be("ZZZ new");
        vm.Tags.Single().Name.Should().Be(vm.Name);
        vm.Books.Should().Equal(sorted);
        vm.ChangedBookIds.Should().BeEquivalentTo(preview.Books.Select(book => book.Id));
        vm.Warnings.Should().HaveCount(1);
        vm.StatusMessage.Should().Be("TagRenameSavedWithWarnings");
    }

    [Fact]
    public async Task Late_search_result_and_disposed_result_are_discarded()
    {
        var repository = new TagFake();
        using var vm = new TagManagementViewModel(repository, repository, key => key);
        var pending = new TaskCompletionSource<TagPage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        repository.Search = text => { if (text != "Slow") return Task.FromResult(new TagPage([new(TagId, text, 1)], 1)); started.SetResult(); return pending.Task; };
        var slow = vm.SearchAsync("Slow");
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await vm.SearchAsync("Fast");
        pending.SetResult(new([new(TagId, "Slow", 1)], 1));
        await slow;
        vm.Tags.Single().Name.Should().Be("Fast");
        repository.Search = _ => { vm.Dispose(); return Task.FromResult(new TagPage([new(TagId, "Disposed", 1)], 1)); };
        await vm.SearchAsync("Disposed");
        vm.Tags.Single().Name.Should().Be("Fast");
    }

    [Fact]
    public async Task Saving_blocks_other_operations_and_rejected_rename_does_not_report_changes()
    {
        var repository = new TagFake();
        using var vm = new TagManagementViewModel(repository, repository, key => key);
        await vm.SelectAsync(new(TagId, "Old", 2));
        var preview = (await vm.PreviewRenameAsync())!;
        var pending = new TaskCompletionSource<TagRenameCompletion>(TaskCreationOptions.RunContinuationsAsynchronously);
        repository.Rename = (_, _) => pending.Task;
        var saving = vm.RenameAsync(preview, "New");
        vm.CanBrowse.Should().BeFalse();
        vm.CanRename.Should().BeFalse();
        await vm.SelectAsync(new(Guid.NewGuid(), "Other", 1));
        (await vm.RenameAsync(preview, "Another")).Should().BeFalse();
        vm.Name.Should().Be("Old");
        pending.SetResult(new(new(TagRenameStatus.NameExists, TagId, "New", []), []));
        (await saving).Should().BeFalse();
        vm.StatusMessage.Should().Be("TagRenameNameExists");
        vm.ChangedBookIds.Should().BeEmpty();
        vm.IsBusy.Should().BeFalse();
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("  ", false)]
    [InlineData("Old", false)]
    [InlineData("old", true)]
    [InlineData(" New ", true)]
    [InlineData("One;Two", false)]
    [InlineData("One\nTwo", false)]
    public void Rename_dialog_checks_name_and_allows_case_correction(string name, bool expected)
    {
        var vm = new TagRenameDialogViewModel(new(TagId, "Old", [new(Guid.NewGuid(), "Book")]), key => key);
        vm.NewName = name;
        vm.CanSave.Should().Be(expected);
    }

    [Fact]
    public async Task Failure_showing_result_after_commit_keeps_changed_ids_and_does_not_claim_rollback()
    {
        var repository = new TagFake();
        using var vm = new TagManagementViewModel(repository, repository,
            key => key == "TagRenameSavedWithWarnings" ? "{invalid format" : key);
        await vm.SelectAsync(new(TagId, "Old", 2));
        var preview = (await vm.PreviewRenameAsync())!;
        (await vm.RenameAsync(preview, "New")).Should().BeTrue();
        vm.StatusMessage.Should().Be("TagRenameRefreshFailed");
        vm.Name.Should().Be("New");
        vm.ChangedBookIds.Should().BeEquivalentTo(preview.Books.Select(book => book.Id));
    }

    [Fact]
    public async Task Book_pages_are_loaded_on_demand_and_stale_identity_cannot_be_renamed()
    {
        var repository = new TagFake();
        var all = Enumerable.Range(0, 205).Select(index => new TagBook(Guid.NewGuid(), $"Book {index}", "Author")).ToArray();
        repository.List = (_, skip, take) => Task.FromResult(new TagBookPage(all.Skip(skip).Take(take).ToArray(), all.Length));
        using var vm = new TagManagementViewModel(repository, repository, key => key);
        await vm.SelectAsync(new(TagId, "Old", all.Length));
        vm.Books.Should().HaveCount(200);
        vm.HasMoreBooks.Should().BeTrue();
        await vm.LoadMoreBooksAsync();
        vm.Books.Should().Equal(all);
        vm.HasMoreBooks.Should().BeFalse();
        (await vm.RenameAsync(new(Guid.NewGuid(), "Old", []), "New")).Should().BeFalse();
        vm.ChangedBookIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Failed_book_load_recovers_controls_and_does_not_expose_exception_text()
    {
        var repository = new TagFake { List = (_, _, _) => throw new IOException("Sensitive path") };
        using var vm = new TagManagementViewModel(repository, repository, key => key);
        await vm.SelectAsync(new(TagId, "Old", 2));
        vm.IsBusy.Should().BeFalse();
        vm.CanRename.Should().BeFalse();
        vm.StatusMessage.Should().Be("TagLoadFailed");
    }

    [Fact]
    public async Task Preview_rejects_a_name_changed_elsewhere_instead_of_allowing_a_silent_failed_save()
    {
        var repository = new TagFake();
        using var vm = new TagManagementViewModel(repository, repository, key => key);
        await vm.SelectAsync(new(TagId, "Old", 2));
        repository.PreviewName = "Changed elsewhere";
        (await vm.PreviewRenameAsync()).Should().BeNull();
        vm.StatusMessage.Should().Be("TagRenameConflict");
        vm.CanBrowse.Should().BeTrue();
        vm.ChangedBookIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Index_refresh_failure_after_success_is_reported_as_saved_and_keeps_changed_ids()
    {
        var repository = new TagFake();
        using var vm = new TagManagementViewModel(repository, repository, key => key);
        await vm.SelectAsync(new(TagId, "Old", 2));
        var preview = (await vm.PreviewRenameAsync())!;
        repository.Search = _ => throw new IOException("Sensitive path");
        (await vm.RenameAsync(preview, "New")).Should().BeTrue();
        vm.Name.Should().Be("New");
        vm.ChangedBookIds.Should().BeEquivalentTo(preview.Books.Select(book => book.Id));
        vm.StatusMessage.Should().Be("TagRenameRefreshFailed");
        vm.IsError.Should().BeTrue();
    }

    private sealed class TagFake : ITagRepository, ITagRenameService
    {
        private readonly TagSourceBook[] source = [new(Guid.NewGuid(), "One"), new(Guid.NewGuid(), "Two")];
        public Func<string, Task<TagPage>> Search { get; set; } = text => Task.FromResult(new TagPage([new(TagId, text, 2)], 1));
        public Func<TagRenamePreview, string, Task<TagRenameCompletion>>? Rename { get; set; }
        public Func<Guid, int, int, Task<TagBookPage>>? List { get; set; }
        public string PreviewName { get; set; } = "Old";
        public Task<TagPage> SearchAsync(string query, int skip, int take, CancellationToken token) => Search(query);
        public Task<TagBookPage> ListBooksAsync(Guid tagId, int skip, int take, CancellationToken token) => List?.Invoke(tagId, skip, take) ?? Task.FromResult(new TagBookPage(
            source.Select((book, index) => new TagBook(book.Id, book.Title, "Author")).ToArray(), 2));
        public Task<TagRenamePreview> PreviewAsync(Guid tagId, CancellationToken token) => Task.FromResult(new TagRenamePreview(TagId, PreviewName, source));
        public Task<TagRenameCompletion> RenameAsync(TagRenamePreview preview, string name, CancellationToken token) => Rename?.Invoke(preview, name)
            ?? Task.FromResult(new TagRenameCompletion(new(TagRenameStatus.Renamed, TagId, name, source), [source[0]]));
        public Task<TagRenamePreview> PreviewRenameAsync(Guid tagId, CancellationToken token) => PreviewAsync(tagId, token);
        Task<TagRenameResult> ITagRepository.RenameAsync(TagRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
    }
}

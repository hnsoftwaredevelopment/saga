using EbookManager.Application.Series;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Series;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class SeriesManagementViewModelTests
{
    [Fact]
    public async Task Rename_keeps_ordered_books_selects_new_query_and_reports_committed_ids_and_warnings()
    {
        var repository = new SeriesFake();
        using var vm = new SeriesManagementViewModel(repository, repository, key => key);
        await vm.SearchAsync("Old");
        await vm.SelectAsync(new("Old", 2));
        var sorted = vm.Books.ToArray();
        var preview = (await vm.PreviewRenameAsync())!;
        await vm.RenameAsync(preview, "ZZZ new");
        vm.Query.Should().Be("ZZZ new");
        vm.Name.Should().Be("ZZZ new");
        vm.Series.Single().Name.Should().Be(vm.Name);
        vm.Books.Should().Equal(sorted);
        vm.ChangedBookIds.Should().BeEquivalentTo(preview.Books.Select(book => book.Id));
        vm.Warnings.Should().HaveCount(1);
        vm.StatusMessage.Should().Be("SeriesRenameSavedWithWarnings");
    }

    [Fact]
    public async Task Late_search_result_and_disposed_result_are_discarded()
    {
        var repository = new SeriesFake();
        using var vm = new SeriesManagementViewModel(repository, repository, key => key);
        var pending = new TaskCompletionSource<SeriesPage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        repository.Search = text => { if (text != "Slow") return Task.FromResult(new SeriesPage([new(text, 1)], 1)); started.SetResult(); return pending.Task; };
        var slow = vm.SearchAsync("Slow");
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await vm.SearchAsync("Fast");
        pending.SetResult(new([new("Slow", 1)], 1));
        await slow;
        vm.Series.Single().Name.Should().Be("Fast");
        repository.Search = _ => { vm.Dispose(); return Task.FromResult(new SeriesPage([new("Disposed", 1)], 1)); };
        await vm.SearchAsync("Disposed");
        vm.Series.Single().Name.Should().Be("Fast");
    }

    [Fact]
    public async Task Saving_blocks_other_operations_and_rejected_rename_does_not_report_changes()
    {
        var repository = new SeriesFake();
        using var vm = new SeriesManagementViewModel(repository, repository, key => key);
        await vm.SelectAsync(new("Old", 2));
        var preview = (await vm.PreviewRenameAsync())!;
        var pending = new TaskCompletionSource<SeriesRenameCompletion>(TaskCreationOptions.RunContinuationsAsynchronously);
        repository.Rename = (_, _) => pending.Task;
        var saving = vm.RenameAsync(preview, "New");
        vm.CanBrowse.Should().BeFalse();
        vm.CanRename.Should().BeFalse();
        await vm.SelectAsync(new("Other", 1));
        (await vm.RenameAsync(preview, "Another")).Should().BeFalse();
        vm.Name.Should().Be("Old");
        pending.SetResult(new(new(SeriesRenameStatus.NameExists, "New", []), []));
        (await saving).Should().BeFalse();
        vm.StatusMessage.Should().Be("SeriesRenameNameExists");
        vm.ChangedBookIds.Should().BeEmpty();
        vm.IsBusy.Should().BeFalse();
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("  ", false)]
    [InlineData("Old", false)]
    [InlineData("old", true)]
    [InlineData(" New ", true)]
    public void Rename_dialog_checks_name_and_allows_case_correction(string name, bool expected)
    {
        var vm = new SeriesRenameDialogViewModel(new("Old", [new(Guid.NewGuid(), "Old", "Book")]), key => key);
        vm.NewName = name;
        vm.CanSave.Should().Be(expected);
    }

    [Fact]
    public async Task Failure_showing_result_after_commit_keeps_changed_ids_and_does_not_claim_rollback()
    {
        var repository = new SeriesFake();
        using var vm = new SeriesManagementViewModel(repository, repository,
            key => key == "SeriesRenameSavedWithWarnings" ? "{invalid format" : key);
        await vm.SelectAsync(new("Old", 2));
        var preview = (await vm.PreviewRenameAsync())!;
        (await vm.RenameAsync(preview, "New")).Should().BeTrue();
        vm.StatusMessage.Should().Be("SeriesRenameRefreshFailed");
        vm.Name.Should().Be("New");
        vm.ChangedBookIds.Should().BeEquivalentTo(preview.Books.Select(book => book.Id));
    }

    private sealed class SeriesFake : ISeriesRepository, ISeriesRenameService
    {
        private readonly SeriesSourceBook[] source = [new(Guid.NewGuid(), "Old", "One"), new(Guid.NewGuid(), "Old", "Two")];
        public Func<string, Task<SeriesPage>> Search { get; set; } = text => Task.FromResult(new SeriesPage([new(text, 2)], 1));
        public Func<SeriesRenamePreview, string, Task<SeriesRenameCompletion>>? Rename { get; set; }
        public Task<SeriesPage> SearchAsync(string query, int skip, int take, CancellationToken token) => Search(query);
        public Task<SeriesBookPage> ListBooksAsync(string name, int skip, int take, CancellationToken token) => Task.FromResult(new SeriesBookPage(
            source.Select((book, index) => new SeriesBook(book.Id, book.Title, "Author", index + 1)).ToArray(), 2));
        public Task<SeriesRenamePreview> PreviewAsync(string name, CancellationToken token) => Task.FromResult(new SeriesRenamePreview(name, source));
        public Task<SeriesRenameCompletion> RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token) => Rename?.Invoke(preview, name)
            ?? Task.FromResult(new SeriesRenameCompletion(new(SeriesRenameStatus.Renamed, name, source), [source[0]]));
        public Task<SeriesRenamePreview> PreviewRenameAsync(string name, CancellationToken token) => PreviewAsync(name, token);
        Task<SeriesRenameResult> ISeriesRepository.RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
    }
}

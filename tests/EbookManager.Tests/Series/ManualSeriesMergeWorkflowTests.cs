using EbookManager.Application.Series;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Series;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.Series;

public sealed class ManualSeriesMergeWorkflowTests
{
    [Fact]
    public async Task Manual_selection_survives_paging_blocks_book_edits_and_clears_on_a_new_search()
    {
        var store = new Store(205); using var vm = new SeriesManagementViewModel(store, store, key => key, store);
        await vm.SearchAsync(""); await vm.SelectAsync(vm.Series[0]); vm.CanRename.Should().BeTrue();
        vm.SetMergeSeries(vm.Series.Take(2)); vm.CanMergeSelectedSeries.Should().BeTrue(); vm.CanBrowseBooks.Should().BeFalse(); vm.CanRename.Should().BeFalse();
        store.HoldSearch = true;
        var loading = vm.SearchAsync("", true); await store.SearchStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        vm.CanChooseSeries.Should().BeFalse(); store.SearchReleased.TrySetResult(); await loading; store.HoldSearch = false;
        vm.CanChooseSeries.Should().BeTrue(); vm.Series.Should().HaveCount(205); vm.MergeSeriesCount.Should().Be(2);
        await vm.SearchAsync("Series"); vm.MergeSeriesCount.Should().Be(0); vm.CanMergeSelectedSeries.Should().BeFalse();
    }
    [Fact]
    public async Task Merge_three_selected_series_accepts_a_new_name_and_refreshes_the_unfiltered_target()
    {
        var store = new Store(); using var vm = new SeriesManagementViewModel(store, store, key => key, store);
        await vm.SearchAsync("Series"); vm.SetMergeSeries(vm.Series.Take(3));
        var preview = (await vm.PreviewSelectedSeriesAsync())!; preview.AllowNewName.Should().BeTrue(); preview.Groups.Should().HaveCount(3);
        (await vm.MergeCandidateAsync(preview, "New name")).Should().BeTrue();
        vm.Name.Should().Be("New name"); vm.Query.Should().BeEmpty(); vm.TotalBooks.Should().Be(3);
        vm.MergeSeriesCount.Should().Be(1); vm.CanMergeSelectedSeries.Should().BeFalse(); vm.CanBrowseBooks.Should().BeTrue(); vm.CanRename.Should().BeTrue();
        vm.ChangedBookIds.Should().HaveCount(3); vm.Books.Select(book => book.Number).Should().Equal(1m, 2m, 3m);
        (await vm.MergeCandidateAsync(preview, "Other")).Should().BeFalse(); store.Saves.Should().Be(1);
    }
    [Fact]
    public async Task Changing_the_series_selection_invalidates_an_old_confirmation()
    {
        var store = new Store(); using var vm = new SeriesManagementViewModel(store, store, key => key, store);
        await vm.SearchAsync(""); vm.SetMergeSeries(vm.Series.Take(2)); var preview = (await vm.PreviewSelectedSeriesAsync())!;
        vm.SetMergeSeries(vm.Series.Skip(1).Take(2)); (await vm.MergeCandidateAsync(preview, "New")).Should().BeFalse(); store.Saves.Should().Be(0);
    }
    [Theory]
    [InlineData(SeriesMergeStatus.NameExists, "SeriesManualMergeNameExists")][InlineData(SeriesMergeStatus.Conflict, "SeriesMergeConflict")]
    public async Task Rejected_merge_preserves_selection_query_and_books_without_reporting_changed_ids(SeriesMergeStatus status, string message)
    {
        var store = new Store { Status = status }; using var vm = new SeriesManagementViewModel(store, store, key => key, store);
        await vm.SearchAsync("Series"); vm.SetMergeSeries(vm.Series.Take(2)); var preview = (await vm.PreviewSelectedSeriesAsync())!;
        (await vm.MergeCandidateAsync(preview, "Existing")).Should().BeFalse();
        vm.MergeSeriesCount.Should().Be(2); vm.Query.Should().Be("Series"); vm.ChangedBookIds.Should().BeEmpty(); vm.StatusMessage.Should().Be(message); vm.IsError.Should().BeTrue();
    }
    [Fact]
    public async Task Committed_merge_preserves_ids_and_warnings_when_reloading_fails()
    {
        var store = new Store { FailRefresh = true, Warning = true }; using var vm = new SeriesManagementViewModel(store, store, key => key, store);
        await vm.SearchAsync(""); vm.SetMergeSeries(vm.Series.Take(2)); var preview = (await vm.PreviewSelectedSeriesAsync())!;
        (await vm.MergeCandidateAsync(preview, "New")).Should().BeTrue();
        vm.ChangedBookIds.Should().HaveCount(2); vm.Warnings.Should().HaveCount(1); vm.StatusMessage.Should().Be("SeriesMergeRefreshFailed"); vm.IsError.Should().BeTrue();
    }
    private sealed class Store(int count = 3) : ISeriesRepository, ISeriesRenameService, ISeriesMergeService
    {
        private List<SeriesMergeBook> rows = Enumerable.Range(1,count).Select(i => new SeriesMergeBook(Guid.NewGuid(), $"Series {i:000}", $"Book {i:000}", "Author", i)).ToList();
        public SeriesMergeStatus Status = SeriesMergeStatus.Merged; public int Saves; public bool FailRefresh, Warning;
        public bool HoldSearch;
        public TaskCompletionSource SearchStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SearchReleased { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<SeriesPage> SearchAsync(string query, int skip, int take, CancellationToken token)
        {
            if (FailRefresh && Saves > 0) throw new IOException();
            if (HoldSearch) { SearchStarted.TrySetResult(); await SearchReleased.Task; }
            var groups = rows.GroupBy(row => row.StoredName).Select(group => new SeriesSummary(group.Key, group.Count())).Where(row => row.Name.Contains(query)).OrderBy(row => row.Name).ToArray();
            return new SeriesPage(groups.Skip(skip).Take(take).ToArray(), groups.Length);
        }
        public Task<SeriesBookPage> ListBooksAsync(string name, int skip, int take, CancellationToken token)
        { var all = rows.Where(row => row.StoredName == name).OrderBy(row => row.Number).ToArray(); return Task.FromResult(new SeriesBookPage(all.Skip(skip).Take(take).Select(row => new SeriesBook(row.Id, row.Title, row.Authors, row.Number)).ToArray(), all.Length)); }
        public Task<SeriesMergePreview?> PreviewAsync(IReadOnlyCollection<string> names, CancellationToken token) => Task.FromResult<SeriesMergePreview?>(new(names.Select(name => new SeriesMergeGroup(name, rows.Where(row => row.StoredName == name).ToArray())).ToArray()));
        public Task<SeriesMergeCompletion> MergeAsync(SeriesMergePreview preview, string target, CancellationToken token)
        {
            Saves++; var changed = preview.Groups.SelectMany(group => group.Books).ToArray();
            if (Status == SeriesMergeStatus.Merged) { var ids = changed.Select(row => row.Id).ToHashSet(); rows = rows.Select(row => ids.Contains(row.Id) ? row with { StoredName = target } : row).ToList(); }
            return Task.FromResult(new SeriesMergeCompletion(new(Status, target, Status == SeriesMergeStatus.Merged ? changed : []), Warning ? changed.Take(1).Select(row => new SeriesSourceBook(row.Id, row.StoredName, row.Title)).ToArray() : []));
        }
        public Task<SeriesRenamePreview> PreviewAsync(string name, CancellationToken token) => throw new NotSupportedException();
        public Task<SeriesRenamePreview> PreviewRenameAsync(string name, CancellationToken token) => throw new NotSupportedException();
        public Task<SeriesRenameResult> RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
        Task<SeriesRenameCompletion> ISeriesRenameService.RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
    }
}

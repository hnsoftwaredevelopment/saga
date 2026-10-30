using EbookManager.Application.Series;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Series;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.Series;

public sealed class SeriesMergeWorkflowTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Merge_refreshes_all_series_keeps_target_and_warnings_even_if_reload_fails(bool failRefresh)
    {
        var fake = new Store();
        using var vm = new SeriesManagementViewModel(fake, fake, key => key, fake);
        await vm.SearchAsync("Bob-");
        var preview = (await vm.PreviewCandidateAsync(new(new("Bob-Evers", 1), new("Bob Evers", 1))))!;
        fake.FailRefresh = failRefresh;
        (await vm.MergeCandidateAsync(preview, "Bob Evers")).Should().BeTrue();
        vm.Name.Should().Be("Bob Evers");
        vm.Query.Should().BeEmpty();
        vm.ChangedBookIds.Should().Equal(Store.First.Id);
        vm.Warnings.Should().HaveCount(1);
        vm.StatusMessage.Should().Be(failRefresh ? "SeriesMergeRefreshFailed" : "SeriesMergeSavedWithWarnings");
        vm.IsError.Should().BeTrue();
        if (!failRefresh)
        {
            vm.Series.Should().HaveCount(2);
            vm.TotalBooks.Should().Be(2);
            vm.Books.Select(book => book.Number).Should().Equal(1m, 2m);
        }
        (await vm.MergeCandidateAsync(preview, "Bob Evers")).Should().BeFalse();
        fake.Merges.Should().Be(1);
    }

    [Fact]
    public async Task Unconfirmed_or_invalid_target_never_saves_and_conflict_preserves_manager_selection()
    {
        var fake = new Store();
        using var vm = new SeriesManagementViewModel(fake, fake, key => key, fake);
        await vm.SearchAsync("Bob-");
        await vm.SelectAsync(new("Bob-Evers", 1));
        (await vm.MergeCandidateAsync(Store.Preview, "Bob Evers")).Should().BeFalse();
        var preview = (await vm.PreviewCandidateAsync(new(new("Bob-Evers", 1), new("Bob Evers", 1))))!;
        (await vm.MergeCandidateAsync(preview, "New name")).Should().BeFalse();
        fake.Conflict = true;
        (await vm.MergeCandidateAsync(preview, "Bob Evers")).Should().BeFalse();
        vm.ChangedBookIds.Should().BeEmpty();
        vm.Name.Should().Be("Bob-Evers");
        vm.Query.Should().Be("Bob-");
        vm.StatusMessage.Should().Be("SeriesMergeConflict");
        fake.Merges.Should().Be(1);
    }

    [Fact]
    public async Task Disposal_after_commit_still_reports_changed_book_ids()
    {
        var fake = new Store();
        using var vm = new SeriesManagementViewModel(fake, fake, key => key, fake);
        var preview = (await vm.PreviewCandidateAsync(new(new("Bob-Evers", 1), new("Bob Evers", 1))))!;
        fake.OnMerge = vm.Dispose;
        (await vm.MergeCandidateAsync(preview, "Bob Evers")).Should().BeTrue();
        vm.ChangedBookIds.Should().Equal(Store.First.Id);
    }

    [Fact]
    public async Task Discovery_caches_names_filters_both_sides_and_only_selected_current_rows_can_compare()
    {
        var fake = new Store();
        using var vm = new SeriesDuplicatesViewModel(new SeriesDuplicateSearchService(fake), key => key);
        await vm.ReloadAsync();
        vm.Candidates.Should().HaveCount(1);
        vm.CanCompare.Should().BeFalse();
        vm.Selected = vm.Candidates.Single();
        vm.CanCompare.Should().BeTrue();
        vm.LastActionMessage = "Saved";
        await vm.SearchAsync("Bob-");
        await vm.SearchAsync("absent");
        vm.CanCompare.Should().BeFalse();
        vm.Candidates.Should().BeEmpty();
        vm.LastActionMessage.Should().Be("Saved");
        fake.Searches.Should().Be(1);
        await vm.ReloadAsync("");
        fake.Searches.Should().Be(2);
    }

    [Fact]
    public async Task Disposed_discovery_ignores_late_index_results()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<SeriesSummary>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var vm = new SeriesDuplicatesViewModel(new DelayedIndex(pending.Task), key => key);
        var loading = vm.ReloadAsync();
        vm.Dispose();
        pending.SetResult([new("Bob Evers", 1), new("Bob-Evers", 1)]);
        await loading;
        vm.Candidates.Should().BeEmpty();
    }

    private sealed class DelayedIndex(Task<IReadOnlyList<SeriesSummary>> value) : ISeriesDuplicateIndex
    { public Task<IReadOnlyList<SeriesSummary>> LoadAsync(CancellationToken token) => value; }

    private sealed class Store : ISeriesRepository, ISeriesRenameService, ISeriesMergeService
    {
        public static readonly SeriesMergeBook First = new(Guid.NewGuid(), "Bob-Evers", "First", "Author", 1);
        public static readonly SeriesMergeBook Second = new(Guid.NewGuid(), "Bob Evers", "Second", "Other", 2);
        public static readonly SeriesMergePreview Preview = new([new("Bob-Evers", [First]), new("Bob Evers", [Second])]);
        public bool FailRefresh, Conflict;
        public int Searches, Merges;
        public Action? OnMerge;
        public Task<SeriesPage> SearchAsync(string query, int skip, int take, CancellationToken token)
        {
            Searches++;
            if (FailRefresh && Merges > 0) throw new IOException("Simulated refresh failure");
            SeriesSummary[] rows = Merges > 0 ? [new("Bob Evers", 2), new("Other", 1)] : [new("Bob-Evers", 1), new("Bob Evers", 1), new("Other", 1)];
            rows = rows.Where(row => row.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
            return Task.FromResult(new SeriesPage(rows.Skip(skip).Take(take).ToArray(), rows.Length));
        }
        public Task<SeriesBookPage> ListBooksAsync(string name, int skip, int take, CancellationToken token) => Task.FromResult(new SeriesBookPage(
            new[] { First, Second }.Where(book => Merges > 0 || book.StoredName == name).Select(book => new SeriesBook(book.Id, book.Title, book.Authors, book.Number)).ToArray(), Merges > 0 ? 2 : 1));
        public Task<SeriesMergePreview?> PreviewAsync(IReadOnlyCollection<string> names, CancellationToken token) => Task.FromResult<SeriesMergePreview?>(Preview);
        public Task<SeriesMergeCompletion> MergeAsync(SeriesMergePreview preview, string target, CancellationToken token)
        {
            Merges++; OnMerge?.Invoke();
            return Task.FromResult(new SeriesMergeCompletion(new(Conflict ? SeriesMergeStatus.Conflict : SeriesMergeStatus.Merged, target, Conflict ? [] : [First]), [new(First.Id, First.StoredName, First.Title)]));
        }
        public Task<SeriesRenamePreview> PreviewAsync(string name, CancellationToken token) => throw new NotSupportedException();
        public Task<SeriesRenamePreview> PreviewRenameAsync(string name, CancellationToken token) => throw new NotSupportedException();
        public Task<SeriesRenameResult> RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
        Task<SeriesRenameCompletion> ISeriesRenameService.RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
    }
}

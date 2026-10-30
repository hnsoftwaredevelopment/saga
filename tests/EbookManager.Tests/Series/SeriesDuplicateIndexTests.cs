using EbookManager.Application.Series;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Series;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.Series;

public sealed class SeriesDuplicateIndexTests
{
    [Fact]
    public async Task Loads_all_lightweight_pages_without_book_or_preview_queries()
    {
        var repository = new Pages();
        var rows = await new SeriesDuplicateSearchService(repository).LoadAsync(default);
        rows.Should().HaveCount(602);
        repository.Skips.Should().Equal(0, 500);
        SeriesDuplicateFinder.Find(rows, "Bob").Candidates.Should().ContainSingle();
    }

    [Fact]
    public async Task Typing_while_loading_uses_latest_query_and_search_retains_action_warnings()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<SeriesSummary>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var vm = new SeriesDuplicatesViewModel(new Pending(pending.Task), key => key);
        var loading = vm.ReloadAsync();
        await vm.SearchAsync("absent");
        vm.LastActionMessage = "Saved with warning";
        vm.Warnings = [new(Guid.NewGuid(), "Bob-Evers", "Book")];
        pending.SetResult([new("Bob Evers", 1), new("Bob-Evers", 1)]);
        await loading;
        vm.Candidates.Should().BeEmpty();
        await vm.SearchAsync("Bob");
        vm.Candidates.Should().ContainSingle();
        vm.Warnings.Should().HaveCount(1);
        vm.LastActionMessage.Should().Be("Saved with warning");
    }

    private sealed class Pending(Task<IReadOnlyList<SeriesSummary>> task) : ISeriesDuplicateIndex
    { public Task<IReadOnlyList<SeriesSummary>> LoadAsync(CancellationToken token) => task; }
    private sealed class Pages : ISeriesRepository
    {
        private readonly SeriesSummary[] rows = Enumerable.Range(0, 600).Select(index => new SeriesSummary($"Other {index}", 1))
            .Concat([new("Bob Evers", 1), new("Bob-Evers", 2)]).ToArray();
        public List<int> Skips { get; } = [];
        public Task<SeriesPage> SearchAsync(string query, int skip, int take, CancellationToken token)
        { token.ThrowIfCancellationRequested(); Skips.Add(skip); return Task.FromResult(new SeriesPage(rows.Skip(skip).Take(take).ToArray(), rows.Length)); }
        public Task<SeriesBookPage> ListBooksAsync(string name, int skip, int take, CancellationToken token) => throw new NotSupportedException();
        public Task<SeriesRenamePreview> PreviewRenameAsync(string name, CancellationToken token) => throw new NotSupportedException();
        public Task<SeriesRenameResult> RenameAsync(SeriesRenamePreview preview, string newName, CancellationToken token) => throw new NotSupportedException();
    }
}

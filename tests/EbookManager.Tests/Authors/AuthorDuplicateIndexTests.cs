using EbookManager.Application.Authors;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Authors;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.Authors;

public sealed class AuthorDuplicateIndexTests
{
    [Fact]
    public async Task Reads_light_pages_beyond_500_without_profiles_books_or_photos()
    {
        var repository = new Pages();
        var authors = await new AuthorDuplicateSearchService(repository).LoadAsync(default);
        authors.Should().HaveCount(602);
        repository.Skips.Should().Equal(0, 500);
        AuthorDuplicateFinder.Find(authors, "Nowee").Candidates.Should().ContainSingle();
    }

    [Fact]
    public async Task Search_uses_cached_names_clears_selection_and_keeps_action_warnings_on_reload()
    {
        var source = new Pages();
        using var vm = new AuthorDuplicatesViewModel(new AuthorDuplicateSearchService(source), key => key);
        await vm.ReloadAsync();
        await vm.SearchAsync("Nowee");
        var reads = source.Skips.Count;
        vm.Candidates.Should().ContainSingle();
        vm.Selected = vm.Candidates.Single();
        vm.CanCompare.Should().BeTrue();
        vm.IsComparing = true; vm.CanCompare.Should().BeFalse(); vm.IsComparing = false;
        await vm.SearchAsync("absent");
        vm.Selected.Should().BeNull(); vm.Candidates.Should().BeEmpty(); vm.CanCompare.Should().BeFalse();
        source.Skips.Should().HaveCount(reads);
        vm.LastActionMessage = "Saved with warning";
        vm.Warnings = [new(Guid.NewGuid(), "Book", null)];
        await vm.ReloadAsync();
        vm.LastActionMessage.Should().Be("Saved with warning"); vm.Warnings.Should().HaveCount(1);
    }

    [Fact]
    public async Task Typing_during_loading_uses_the_latest_query_and_disposal_discards_late_results()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<AuthorSummary>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var vm = new AuthorDuplicatesViewModel(new PendingIndex(pending.Task), key => key);
        var load = vm.ReloadAsync();
        await vm.SearchAsync("absent");
        pending.SetResult([new(Guid.NewGuid(), "J. Nowee", 1), new(Guid.NewGuid(), "Johannes Nowee", 1)]);
        await load;
        vm.Candidates.Should().BeEmpty();
        pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var disposed = new AuthorDuplicatesViewModel(new PendingIndex(pending.Task), key => key);
        var late = disposed.ReloadAsync(); disposed.Dispose();
        pending.SetResult([new(Guid.NewGuid(), "J. Nowee", 1), new(Guid.NewGuid(), "Johannes Nowee", 1)]);
        await late; disposed.Candidates.Should().BeEmpty();
    }

    private sealed class PendingIndex(Task<IReadOnlyList<AuthorSummary>> pending) : IAuthorDuplicateIndex
    { public Task<IReadOnlyList<AuthorSummary>> LoadAsync(CancellationToken token) => pending; }
    private sealed class Pages : IAuthorProfileRepository
    {
        private readonly AuthorSummary[] rows = Enumerable.Range(0, 600).Select(i => new AuthorSummary(Guid.NewGuid(), $"Unrelated {i}", 1))
            .Concat([new(Guid.NewGuid(), "J. Nowee", 1), new(Guid.NewGuid(), "Johannes Nowee", 2)]).ToArray();
        public List<int> Skips { get; } = [];
        public Task<AuthorPage> SearchAsync(string query, int skip, int take, CancellationToken token)
        { token.ThrowIfCancellationRequested(); Skips.Add(skip); return Task.FromResult(new AuthorPage(rows.Skip(skip).Take(take).ToArray(), rows.Length)); }
        public Task<AuthorProfile?> GetAsync(Guid id, CancellationToken token) => throw new NotSupportedException();
        public Task<IReadOnlyList<AuthorBook>> ListBooksAsync(Guid id, int skip, int take, CancellationToken token) => throw new NotSupportedException();
        public Task<AuthorProfileSaveStatus> SaveAsync(AuthorProfile original, AuthorProfile updated, CancellationToken token) => throw new NotSupportedException();
    }
}

using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class DuplicateExclusionsViewModelTests
{
    [Fact]
    public async Task LoadAsync_exposes_ignored_duplicate_pairs()
    {
        var firstPair = CreateExclusion("First", "Author A", "Second", "Author B");
        var secondPair = CreateExclusion("Third", "Author C", "Fourth", "Author D");
        var repository = new InMemoryDuplicateExclusionRepository([firstPair, secondPair]);
        var viewModel = new DuplicateExclusionsViewModel(repository);

        await viewModel.LoadAsync();

        viewModel.HasRows.Should().BeTrue();
        viewModel.ExclusionCount.Should().Be(2);
        viewModel.Rows.Select(row => row.FirstBookTitle).Should().Equal("First", "Third");
        viewModel.Rows.Select(row => row.SecondBookAuthors).Should().Equal("Author B", "Author D");
        viewModel.RestoreAllCommand.CanExecute(null).Should().BeTrue();
        viewModel.RestoreSelectedCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task RestoreSelectedCommand_removes_selected_pairs()
    {
        var firstPair = CreateExclusion("First", "Author A", "Second", "Author B");
        var secondPair = CreateExclusion("Third", "Author C", "Fourth", "Author D");
        var repository = new InMemoryDuplicateExclusionRepository([firstPair, secondPair]);
        var viewModel = new DuplicateExclusionsViewModel(repository);
        await viewModel.LoadAsync();

        viewModel.SetSelectedRows([viewModel.Rows[0]]);
        await viewModel.RestoreSelectedCommand.ExecuteAsync(null);

        repository.Exclusions.Select(exclusion => exclusion.Pair).Should().Equal(secondPair.Pair);
        viewModel.Rows.Should().ContainSingle()
            .Which.Pair.Should().Be(secondPair.Pair);
        viewModel.RestoreSelectedCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task RestoreAllCommand_clears_all_pairs()
    {
        var repository = new InMemoryDuplicateExclusionRepository(
        [
            CreateExclusion("First", "Author A", "Second", "Author B"),
            CreateExclusion("Third", "Author C", "Fourth", "Author D")
        ]);
        var viewModel = new DuplicateExclusionsViewModel(repository);
        await viewModel.LoadAsync();

        await viewModel.RestoreAllCommand.ExecuteAsync(null);

        repository.Exclusions.Should().BeEmpty();
        viewModel.HasRows.Should().BeFalse();
        viewModel.ExclusionCount.Should().Be(0);
    }

    private static DuplicateExclusion CreateExclusion(
        string firstTitle,
        string firstAuthor,
        string secondTitle,
        string secondAuthor)
    {
        var pair = DuplicateExclusionPair.Create(Guid.NewGuid(), Guid.NewGuid());
        return new DuplicateExclusion(
            pair,
            firstTitle,
            [firstAuthor],
            secondTitle,
            [secondAuthor],
            DateTimeOffset.UtcNow);
    }

    private sealed class InMemoryDuplicateExclusionRepository(
        IReadOnlyList<DuplicateExclusion> initialExclusions) : IDuplicateExclusionRepository
    {
        private readonly List<DuplicateExclusion> exclusions = [.. initialExclusions];

        public IReadOnlyList<DuplicateExclusion> Exclusions => exclusions;

        public Task<IReadOnlySet<DuplicateExclusionPair>> ListDuplicateExclusionsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<DuplicateExclusionPair>>(exclusions.Select(exclusion => exclusion.Pair).ToHashSet());

        public Task<IReadOnlyList<DuplicateExclusion>> ListDuplicateExclusionDetailsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DuplicateExclusion>>(exclusions.ToArray());

        public Task AddDuplicateExclusionsAsync(
            IReadOnlyCollection<DuplicateExclusionPair> pairs,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task RemoveDuplicateExclusionsAsync(
            IReadOnlyCollection<DuplicateExclusionPair> pairs,
            CancellationToken cancellationToken)
        {
            var pairSet = pairs.ToHashSet();
            exclusions.RemoveAll(exclusion => pairSet.Contains(exclusion.Pair));
            return Task.CompletedTask;
        }

        public Task ClearDuplicateExclusionsAsync(CancellationToken cancellationToken)
        {
            exclusions.Clear();
            return Task.CompletedTask;
        }
    }
}

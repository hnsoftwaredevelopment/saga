using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Metadata;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class MetadataQualityExclusionsViewModelTests
{
    [Fact]
    public async Task LoadAsync_exposes_book_signal_authors_and_date()
    {
        var createdAt = DateTimeOffset.Parse("2026-08-27T14:00:00+00:00");
        var exclusion = CreateExclusion(
            MetadataQualitySignalKeys.MissingAuthor,
            "Needs review",
            ["First Author", "Second Author"],
            createdAt);
        var repository = new InMemoryMetadataQualityExclusionRepository([exclusion]);
        var viewModel = new MetadataQualityExclusionsViewModel(repository, key => $"localized:{key}");

        await viewModel.LoadAsync();

        viewModel.HasRows.Should().BeTrue();
        viewModel.ExclusionCount.Should().Be(1);
        viewModel.Rows.Should().ContainSingle().Which.Should().Match<MetadataQualityExclusionRowViewModel>(row =>
            row.Key == exclusion.Key &&
            row.BookTitle == "Needs review" &&
            row.BookAuthors == "First Author, Second Author" &&
            row.Signal == "localized:MetadataQualityMissingAuthor" &&
            row.CreatedAt == createdAt);
        viewModel.RestoreSelectedCommand.CanExecute(null).Should().BeFalse();
        viewModel.RestoreAllCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task RestoreSelectedCommand_removes_only_selected_keys_and_updates_count()
    {
        var first = CreateExclusion(MetadataQualitySignalKeys.MissingCover, "First");
        var second = CreateExclusion(MetadataQualitySignalKeys.UnknownLanguage, "Second");
        var repository = new InMemoryMetadataQualityExclusionRepository([first, second]);
        var viewModel = new MetadataQualityExclusionsViewModel(repository, key => key);
        await viewModel.LoadAsync();

        viewModel.SetSelectedRows([viewModel.Rows[1]]);
        viewModel.RestoreSelectedCommand.CanExecute(null).Should().BeTrue();
        await viewModel.RestoreSelectedCommand.ExecuteAsync(null);

        repository.Exclusions.Should().ContainSingle().Which.Should().Be(first);
        viewModel.Rows.Should().ContainSingle().Which.Key.Should().Be(first.Key);
        viewModel.ExclusionCount.Should().Be(1);
        viewModel.RestoreSelectedCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task RestoreAllCommand_clears_every_exclusion()
    {
        var repository = new InMemoryMetadataQualityExclusionRepository(
        [
            CreateExclusion(MetadataQualitySignalKeys.MissingCover, "First"),
            CreateExclusion(MetadataQualitySignalKeys.MessyTags, "Second")
        ]);
        var viewModel = new MetadataQualityExclusionsViewModel(repository, key => key);
        await viewModel.LoadAsync();

        await viewModel.RestoreAllCommand.ExecuteAsync(null);

        repository.Exclusions.Should().BeEmpty();
        viewModel.Rows.Should().BeEmpty();
        viewModel.HasRows.Should().BeFalse();
        viewModel.ExclusionCount.Should().Be(0);
        viewModel.RestoreAllCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_shows_raw_unknown_signal_key()
    {
        var exclusion = CreateExclusion("future-signal", "Future book");
        var repository = new InMemoryMetadataQualityExclusionRepository([exclusion]);
        var viewModel = new MetadataQualityExclusionsViewModel(repository, key => $"localized:{key}");

        await viewModel.LoadAsync();

        viewModel.Rows.Should().ContainSingle().Which.Signal.Should().Be("future-signal");
    }

    private static MetadataQualityExclusion CreateExclusion(
        string signalKey,
        string title,
        IReadOnlyList<string>? authors = null,
        DateTimeOffset? createdAt = null) =>
        new(
            new MetadataQualityExclusionKey(Guid.NewGuid(), signalKey),
            title,
            authors ?? ["Author"],
            createdAt ?? DateTimeOffset.UtcNow);

    private sealed class InMemoryMetadataQualityExclusionRepository(
        IReadOnlyList<MetadataQualityExclusion> initialExclusions) : IMetadataQualityExclusionRepository
    {
        private readonly List<MetadataQualityExclusion> exclusions = [.. initialExclusions];

        public IReadOnlyList<MetadataQualityExclusion> Exclusions => exclusions;

        public Task<IReadOnlySet<MetadataQualityExclusionKey>> ListMetadataQualityExclusionsAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<MetadataQualityExclusionKey>>(
                exclusions.Select(exclusion => exclusion.Key).ToHashSet());

        public Task<IReadOnlyList<MetadataQualityExclusion>> ListMetadataQualityExclusionDetailsAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MetadataQualityExclusion>>(exclusions.ToArray());

        public Task AddMetadataQualityExclusionsAsync(
            IReadOnlyCollection<MetadataQualityExclusionKey> keys,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RemoveMetadataQualityExclusionsAsync(
            IReadOnlyCollection<MetadataQualityExclusionKey> keys,
            CancellationToken cancellationToken)
        {
            var keySet = keys.ToHashSet();
            exclusions.RemoveAll(exclusion => keySet.Contains(exclusion.Key));
            return Task.CompletedTask;
        }

        public Task ClearMetadataQualityExclusionsAsync(CancellationToken cancellationToken)
        {
            exclusions.Clear();
            return Task.CompletedTask;
        }
    }
}

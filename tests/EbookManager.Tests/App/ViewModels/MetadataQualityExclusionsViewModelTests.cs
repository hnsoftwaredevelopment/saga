using System.Collections.Specialized;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Metadata;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class MetadataQualityExclusionsViewModelTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Restore_failure_is_reported_without_removing_rows_or_selection(bool restoreAll)
    {
        var repository = new InMemoryMetadataQualityExclusionRepository(
            [CreateExclusion(MetadataQualitySignalKeys.MissingCover, "Book")]) { FailRestore = true };
        var viewModel = new MetadataQualityExclusionsViewModel(repository, key => $"localized:{key}");
        await viewModel.LoadAsync();
        var row = viewModel.Rows.Single();
        viewModel.SetSelectedRows([row]);

        var action = () => (restoreAll ? viewModel.RestoreAllCommand : viewModel.RestoreSelectedCommand).ExecuteAsync(null);
        await action.Should().NotThrowAsync();

        viewModel.StatusMessage.Should().Be("localized:MetadataQualityExclusionsRestoreFailed");
        viewModel.Rows.Should().Equal(row);
        viewModel.SelectedRows.Should().Equal(row);
        repository.Exclusions.Should().ContainSingle();
        viewModel.IsRestoring.Should().BeFalse();
        viewModel.RestoreAllCommand.CanExecute(null).Should().BeTrue();
        viewModel.RestoreSelectedCommand.CanExecute(null).Should().BeTrue();
        repository.FailRestore = false;
        await action();
        viewModel.StatusMessage.Should().BeNull();
        viewModel.Rows.Should().BeEmpty();
        viewModel.SelectedRows.Should().BeEmpty();
        repository.Exclusions.Should().BeEmpty();
    }

    [Fact]
    public async Task Restore_uses_original_selection_and_retains_a_later_selection()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new InMemoryMetadataQualityExclusionRepository(
            [CreateExclusion(MetadataQualitySignalKeys.MissingCover, "First"),
             CreateExclusion(MetadataQualitySignalKeys.MissingCover, "Second")])
            { BeforeRemoveAsync = () => gate.Task };
        var viewModel = new MetadataQualityExclusionsViewModel(repository, key => key);
        await viewModel.LoadAsync();
        var first = viewModel.Rows[0];
        var second = viewModel.Rows[1];
        viewModel.SetSelectedRows([first]);
        var operation = viewModel.RestoreSelectedCommand.ExecuteAsync(null);
        viewModel.IsRestoring.Should().BeTrue();
        viewModel.RestoreAllCommand.CanExecute(null).Should().BeFalse();
        viewModel.SetSelectedRows([second]);
        viewModel.RestoreSelectedCommand.CanExecute(null).Should().BeFalse();
        await viewModel.RestoreAllCommand.ExecuteAsync(null);
        repository.Exclusions.Should().HaveCount(2);
        gate.SetResult();
        await operation;
        viewModel.Rows.Should().Equal(second);
        viewModel.SelectedRows.Should().Equal(second);
        repository.Exclusions.Should().ContainSingle().Which.Key.Should().Be(second.Key);
        viewModel.IsRestoring.Should().BeFalse();
        viewModel.StatusMessage.Should().BeNull();
    }

    [Fact]
    public async Task Cancellation_keeps_rows_and_releases_the_busy_state_without_an_error_message()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var repository = new InMemoryMetadataQualityExclusionRepository(
            [CreateExclusion(MetadataQualitySignalKeys.MissingCover, "Book")])
            { BeforeRemoveAsync = () => Task.FromCanceled(cancellation.Token) };
        var viewModel = new MetadataQualityExclusionsViewModel(repository, key => key);
        await viewModel.LoadAsync();
        var row = viewModel.Rows.Single();
        viewModel.SetSelectedRows([row]);
        var action = () => viewModel.RestoreSelectedCommand.ExecuteAsync(null);
        await action.Should().ThrowAsync<OperationCanceledException>();
        viewModel.IsRestoring.Should().BeFalse();
        viewModel.Rows.Should().Equal(row);
        viewModel.SelectedRows.Should().Equal(row);
        viewModel.StatusMessage.Should().BeNull();
    }

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
        var changes = new List<NotifyCollectionChangedAction>();
        viewModel.Rows.CollectionChanged += (_, args) => changes.Add(args.Action);

        await viewModel.RestoreAllCommand.ExecuteAsync(null);

        repository.Exclusions.Should().BeEmpty();
        viewModel.Rows.Should().BeEmpty();
        viewModel.HasRows.Should().BeFalse();
        viewModel.ExclusionCount.Should().Be(0);
        viewModel.RestoreAllCommand.CanExecute(null).Should().BeFalse();
        changes.Should().Equal(NotifyCollectionChangedAction.Reset);
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
        public bool FailRestore { get; set; }
        public Func<Task>? BeforeRemoveAsync { get; init; }

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

        public async Task RemoveMetadataQualityExclusionsAsync(
            IReadOnlyCollection<MetadataQualityExclusionKey> keys,
            CancellationToken cancellationToken)
        {
            if (FailRestore) { throw new IOException("SQL C:\\private\\library.db"); }
            if (BeforeRemoveAsync is not null) { await BeforeRemoveAsync(); }
            var keySet = keys.ToHashSet();
            exclusions.RemoveAll(exclusion => keySet.Contains(exclusion.Key));
        }

        public Task ClearMetadataQualityExclusionsAsync(CancellationToken cancellationToken)
        {
            if (FailRestore) { throw new IOException("SQL C:\\private\\library.db"); }
            exclusions.Clear();
            return Task.CompletedTask;
        }
    }
}

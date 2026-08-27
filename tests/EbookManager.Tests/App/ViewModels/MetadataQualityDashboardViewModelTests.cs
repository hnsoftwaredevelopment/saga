using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class MetadataQualityDashboardViewModelTests
{
    [Fact]
    public void Constructor_selects_first_book_of_first_non_empty_issue()
    {
        var book = CreateBook("Zonder auteur", ["Unknown"]);

        var dashboard = new MetadataQualityDashboardViewModel([book], key => key);

        dashboard.SelectedIssue!.Title.Should().Be("MetadataQualityMissingAuthor");
        dashboard.SelectedBook.Should().NotBeNull();
        dashboard.SelectedBookId.Should().Be(book.Id);
    }

    [Fact]
    public void Selecting_issue_selects_its_first_book()
    {
        var first = CreateBook("Eerste", ["Unknown"]);
        var second = CreateBook("Tweede", ["Auteur"]);
        var dashboard = new MetadataQualityDashboardViewModel([first, second], key => key);

        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.Title == "MetadataQualityMissingCover");

        dashboard.SelectedBookId.Should().Be(first.Id);
    }

    [Fact]
    public void Selecting_empty_issue_clears_book_selection()
    {
        var book = CreateBook("Zonder auteur", ["Unknown"], coverBytes: [1]);
        var dashboard = new MetadataQualityDashboardViewModel([book], key => key);

        var issueWithBook = dashboard.Issues.Single(issue =>
            issue.Title == "MetadataQualityMissingAuthor");
        dashboard.SelectedIssue = issueWithBook;
        dashboard.SelectedBook = issueWithBook.Rows.Single();

        dashboard.SelectedBook.Should().NotBeNull();
        dashboard.SelectedBookId.Should().Be(book.Id);
        dashboard.CanOpenSelectedBook.Should().BeTrue();

        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.Title == "MetadataQualityMissingCover");

        dashboard.SelectedBook.Should().BeNull();
        dashboard.SelectedBookId.Should().BeNull();
        dashboard.CanOpenSelectedBook.Should().BeFalse();
    }

    [Fact]
    public void Selected_book_enables_open_in_library_action()
    {
        var book = CreateBook("Zonder auteur", ["Unknown"]);

        var dashboard = new MetadataQualityDashboardViewModel([book], key => key);

        dashboard.CanOpenSelectedBook.Should().BeTrue();
    }

    [Fact]
    public void Constructor_filters_only_the_exact_book_and_signal_combination()
    {
        var book = CreateBook("Exact filter", ["Unknown"]);
        var excluded = new MetadataQualityExclusionKey(book.Id, MetadataQualitySignalKeys.MissingAuthor);

        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => key,
            new HashSet<MetadataQualityExclusionKey> { excluded },
            new RecordingMetadataQualityExclusionRepository());

        dashboard.Issues.Single(issue => issue.SignalKey == MetadataQualitySignalKeys.MissingAuthor)
            .Rows.Should().BeEmpty();
        dashboard.Issues.Single(issue => issue.SignalKey == MetadataQualitySignalKeys.MissingCover)
            .Rows.Should().ContainSingle().Which.Id.Should().Be(book.Id);
    }

    [Theory]
    [InlineData(0, "Beta")]
    [InlineData(1, "Gamma")]
    [InlineData(2, "Beta")]
    public async Task Mark_correct_removes_selected_row_and_selects_the_expected_neighbor(
        int selectedIndex,
        string expectedSelectedTitle)
    {
        var repository = new RecordingMetadataQualityExclusionRepository();
        var dashboard = new MetadataQualityDashboardViewModel(
            [
                CreateBook("Alpha", ["Unknown"], coverBytes: [1]),
                CreateBook("Beta", ["Unknown"], coverBytes: [1]),
                CreateBook("Gamma", ["Unknown"], coverBytes: [1])
            ],
            key => key,
            repository: repository);
        var issue = dashboard.Issues.Single(item => item.SignalKey == MetadataQualitySignalKeys.MissingAuthor);
        dashboard.SelectedIssue = issue;
        dashboard.SelectedBook = issue.Rows[selectedIndex];
        var selectedId = dashboard.SelectedBook.Id;

        await dashboard.MarkSelectedIssueCorrectCommand.ExecuteAsync(null);

        issue.Count.Should().Be(2);
        dashboard.TotalIssueCount.Should().Be(2);
        dashboard.SelectedBook!.Title.Should().Be(expectedSelectedTitle);
        dashboard.MarkSelectedIssueCorrectCommand.CanExecute(null).Should().BeTrue();
        repository.AddedKeys.Should().ContainSingle().Which.Should().Be(
            new MetadataQualityExclusionKey(selectedId, MetadataQualitySignalKeys.MissingAuthor));
    }

    [Fact]
    public async Task Mark_correct_clears_selection_and_disables_actions_when_category_becomes_empty()
    {
        var repository = new RecordingMetadataQualityExclusionRepository();
        var dashboard = new MetadataQualityDashboardViewModel(
            [CreateBook("Only row", ["Unknown"], coverBytes: [1])],
            key => key,
            repository: repository);

        await dashboard.MarkSelectedIssueCorrectCommand.ExecuteAsync(null);

        dashboard.SelectedIssue!.Count.Should().Be(0);
        dashboard.TotalIssueCount.Should().Be(0);
        dashboard.HasIssues.Should().BeFalse();
        dashboard.SelectedBook.Should().BeNull();
        dashboard.CanOpenSelectedBook.Should().BeFalse();
        dashboard.MarkSelectedIssueCorrectCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task Mark_correct_keeps_selection_and_sets_localized_status_when_storage_fails()
    {
        var repository = new RecordingMetadataQualityExclusionRepository(new InvalidOperationException("Storage failed"));
        var dashboard = new MetadataQualityDashboardViewModel(
            [CreateBook("Keep row", ["Unknown"], coverBytes: [1])],
            key => $"localized:{key}",
            repository: repository);
        var selectedBook = dashboard.SelectedBook;

        await dashboard.MarkSelectedIssueCorrectCommand.ExecuteAsync(null);

        dashboard.SelectedIssue!.Rows.Should().ContainSingle();
        dashboard.SelectedBook.Should().BeSameAs(selectedBook);
        dashboard.StatusMessage.Should().Be("localized:MetadataQualityMarkCorrectFailed");
    }

    private static Book CreateBook(
        string title,
        IReadOnlyList<string> authors,
        byte[]? coverBytes = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata(title, authors, Language: "nl", CoverBytes: coverBytes),
            ReadingStatus.Unread,
            null,
            now,
            now);
    }

    private sealed class RecordingMetadataQualityExclusionRepository(Exception? addException = null)
        : IMetadataQualityExclusionRepository
    {
        public List<MetadataQualityExclusionKey> AddedKeys { get; } = [];

        public Task<IReadOnlySet<MetadataQualityExclusionKey>> ListMetadataQualityExclusionsAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<MetadataQualityExclusionKey>>(new HashSet<MetadataQualityExclusionKey>());

        public Task<IReadOnlyList<MetadataQualityExclusion>> ListMetadataQualityExclusionDetailsAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MetadataQualityExclusion>>([]);

        public Task AddMetadataQualityExclusionsAsync(
            IReadOnlyCollection<MetadataQualityExclusionKey> keys,
            CancellationToken cancellationToken)
        {
            if (addException is not null)
            {
                return Task.FromException(addException);
            }

            AddedKeys.AddRange(keys);
            return Task.CompletedTask;
        }

        public Task RemoveMetadataQualityExclusionsAsync(
            IReadOnlyCollection<MetadataQualityExclusionKey> keys,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ClearMetadataQualityExclusionsAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}

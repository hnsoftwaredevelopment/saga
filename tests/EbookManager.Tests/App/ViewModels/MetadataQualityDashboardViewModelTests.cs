using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using EbookManager.Application.Metadata;
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

    [Fact]
    public async Task Repair_missing_author_uses_known_authors_and_reevaluates_the_saved_book()
    {
        var missingAuthor = CreateBook("Boek zonder auteur", ["Unknown"]);
        var knownAuthor = CreateBook("Ander boek", ["Karin Slaughter"], coverBytes: [1]);
        var repairedBook = missingAuthor with
        {
            Metadata = new BookMetadata("Boek zonder auteur", ["Karin Slaughter"], Language: "nl")
        };
        var repairService = new RecordingAuthorRepairService(repairedBook);
        MetadataQualityAuthorRepairViewModel? shownRepair = null;
        var dashboard = new MetadataQualityDashboardViewModel(
            [missingAuthor, knownAuthor],
            key => key,
            authorRepairService: repairService,
            showAuthorRepair: (repair, _) =>
            {
                shownRepair = repair;
                repair.AuthorText = "Karin Slaughter";
                return Task.FromResult(true);
            });
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.MissingAuthor);
        dashboard.SelectedBook = dashboard.SelectedIssue.Rows.Single();

        await dashboard.RepairMissingAuthorCommand.ExecuteAsync(null);

        shownRepair.Should().NotBeNull();
        shownRepair!.Suggestions.Should().Contain("Karin Slaughter");
        repairService.BookIds.Should().Equal(missingAuthor.Id);
        repairService.Author.Should().Be("Karin Slaughter");
        dashboard.SelectedIssue.Rows.Should().BeEmpty();
        dashboard.SelectedBook.Should().BeNull();
        dashboard.Issues.Single(issue => issue.SignalKey == MetadataQualitySignalKeys.MissingCover)
            .Rows.Single(row => row.Id == missingAuthor.Id).Authors.Should().Be("Karin Slaughter");
    }

    [Fact]
    public async Task Repair_missing_author_does_not_write_when_dialog_is_cancelled()
    {
        var book = CreateBook("Boek", ["Unknown"], coverBytes: [1]);
        var repairService = new RecordingAuthorRepairService(book);
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => key,
            authorRepairService: repairService,
            showAuthorRepair: (_, _) => Task.FromResult(false));

        await dashboard.RepairMissingAuthorCommand.ExecuteAsync(null);

        repairService.BookIds.Should().BeEmpty();
        dashboard.SelectedIssue!.Rows.Should().ContainSingle();
    }

    [Fact]
    public void Repair_missing_author_is_disabled_for_other_quality_signals()
    {
        var book = CreateBook("Boek", ["Unknown"]);
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => key,
            authorRepairService: new RecordingAuthorRepairService(book),
            showAuthorRepair: (_, _) => Task.FromResult(true));

        dashboard.RepairMissingAuthorCommand.CanExecute(null).Should().BeTrue();
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.MissingCover);

        dashboard.RepairMissingAuthorCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task Repair_missing_author_keeps_existing_signal_exclusions()
    {
        var book = CreateBook("Jan Jansen", ["Unknown"], coverBytes: [1]);
        var repairedBook = book with
        {
            Metadata = new BookMetadata("Jan Jansen", ["Boektitel"], Language: "nl", CoverBytes: [1])
        };
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => key,
            new HashSet<MetadataQualityExclusionKey>
            {
                new(book.Id, MetadataQualitySignalKeys.PossibleTitleAuthorSwap)
            },
            authorRepairService: new RecordingAuthorRepairService(repairedBook),
            showAuthorRepair: (repair, _) =>
            {
                repair.AuthorText = "Boektitel";
                return Task.FromResult(true);
            });

        await dashboard.RepairMissingAuthorCommand.ExecuteAsync(null);

        dashboard.Issues.Single(issue =>
                issue.SignalKey == MetadataQualitySignalKeys.PossibleTitleAuthorSwap)
            .Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task Repair_missing_author_keeps_row_and_sets_status_when_save_fails()
    {
        var book = CreateBook("Boek", ["Unknown"], coverBytes: [1]);
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => $"localized:{key}",
            authorRepairService: new RecordingAuthorRepairService(
                book,
                MetadataQualityAuthorRepairStatus.Failed),
            showAuthorRepair: (repair, _) =>
            {
                repair.AuthorText = "Auteur";
                return Task.FromResult(true);
            });

        await dashboard.RepairMissingAuthorCommand.ExecuteAsync(null);

        dashboard.SelectedIssue!.Rows.Should().ContainSingle();
        dashboard.StatusMessage.Should().Be("localized:MetadataQualityAuthorRepairFailed");
    }

    [Fact]
    public async Task Repair_missing_author_shows_warning_when_author_was_saved_but_writeback_failed()
    {
        var book = CreateBook("Boek", ["Unknown"], coverBytes: [1]);
        var repairedBook = book with
        {
            Metadata = new BookMetadata("Boek", ["Auteur"], Language: "nl", CoverBytes: [1])
        };
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => $"localized:{key}",
            authorRepairService: new RecordingAuthorRepairService(
                repairedBook,
                MetadataQualityAuthorRepairStatus.SavedWithWriteBackErrors),
            showAuthorRepair: (repair, _) =>
            {
                repair.AuthorText = "Auteur";
                return Task.FromResult(true);
            });

        await dashboard.RepairMissingAuthorCommand.ExecuteAsync(null);

        dashboard.SelectedIssue!.Rows.Should().BeEmpty();
        dashboard.StatusMessage.Should().Be("localized:MetadataQualityAuthorRepairWriteBackWarning");
    }

    [Fact]
    public async Task Repair_missing_author_reports_neutrally_when_book_already_has_an_author()
    {
        var staleBook = CreateBook("Boek", ["Unknown"], coverBytes: [1]);
        var currentBook = staleBook with
        {
            Metadata = new BookMetadata("Boek", ["Bestaande Auteur"], Language: "nl", CoverBytes: [1])
        };
        var dashboard = new MetadataQualityDashboardViewModel(
            [staleBook],
            key => $"localized:{key}",
            authorRepairService: new RecordingAuthorRepairService(
                currentBook,
                MetadataQualityAuthorRepairStatus.NotApplicable),
            showAuthorRepair: (repair, _) =>
            {
                repair.AuthorText = "Nieuwe Auteur";
                return Task.FromResult(true);
            });

        await dashboard.RepairMissingAuthorCommand.ExecuteAsync(null);

        dashboard.SelectedIssue!.Rows.Should().BeEmpty();
        dashboard.StatusMessage.Should().Be("localized:MetadataQualityAuthorRepairNotNeeded");
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

    private sealed class RecordingAuthorRepairService(
        Book repairedBook,
        MetadataQualityAuthorRepairStatus status = MetadataQualityAuthorRepairStatus.Succeeded)
        : IMetadataQualityAuthorRepairService
    {
        public IReadOnlyList<Guid> BookIds { get; private set; } = [];
        public string? Author { get; private set; }

        public Task<MetadataQualityAuthorRepairBatchResult> RepairAsync(
            IReadOnlyCollection<Guid> bookIds,
            string author,
            CancellationToken cancellationToken)
        {
            BookIds = bookIds.ToArray();
            Author = author;
            return Task.FromResult(new MetadataQualityAuthorRepairBatchResult(
            [
                new MetadataQualityAuthorRepairItemResult(
                    repairedBook.Id,
                    status,
                    repairedBook)
            ]));
        }
    }
}

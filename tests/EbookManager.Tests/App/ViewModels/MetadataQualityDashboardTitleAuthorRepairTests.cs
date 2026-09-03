using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class MetadataQualityDashboardTitleAuthorRepairTests
{
    [Fact]
    public void Swap_is_enabled_only_for_a_repairable_title_author_signal()
    {
        var book = CreateBook("Jan Jansen", "De verdwenen stad");
        var dashboard = CreateDashboard(book, new RecordingRepairService(book));

        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.PossibleTitleAuthorSwap);
        dashboard.RepairTitleAuthorCommand.CanExecute(null).Should().BeTrue();

        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.MissingCover);
        dashboard.RepairTitleAuthorCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Swap_is_disabled_when_the_only_author_is_not_usable()
    {
        var book = CreateBook("Jan Jansen", "Unknown");
        var dashboard = CreateDashboard(book, new RecordingRepairService(book));
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.PossibleTitleAuthorSwap);

        dashboard.RepairTitleAuthorCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task Swap_shows_the_before_after_preview_and_reevaluates_the_book()
    {
        var book = CreateBook("Jan Jansen", "De verdwenen stad");
        var repairedBook = CreateRepairedBook(book);
        var service = new RecordingRepairService(repairedBook);
        MetadataQualityTitleAuthorRepairViewModel? shownRepair = null;
        Book? notifiedBook = null;
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => key,
            titleAuthorRepairService: service,
            showTitleAuthorRepair: (repair, _) =>
            {
                shownRepair = repair;
                return Task.FromResult(true);
            },
            bookRepaired: repaired => notifiedBook = repaired);
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.PossibleTitleAuthorSwap);

        await dashboard.RepairTitleAuthorCommand.ExecuteAsync(null);

        shownRepair.Should().NotBeNull();
        shownRepair!.CurrentTitle.Should().Be("Jan Jansen");
        shownRepair.CurrentAuthor.Should().Be("De verdwenen stad");
        shownRepair.NewTitle.Should().Be("De verdwenen stad");
        shownRepair.NewAuthor.Should().Be("Jan Jansen");
        service.BookId.Should().Be(book.Id);
        notifiedBook.Should().BeSameAs(repairedBook);
        dashboard.SelectedIssue.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task Swap_does_not_write_when_the_dialog_is_cancelled()
    {
        var book = CreateBook("Jan Jansen", "De verdwenen stad");
        var service = new RecordingRepairService(book);
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => key,
            titleAuthorRepairService: service,
            showTitleAuthorRepair: (_, _) => Task.FromResult(false));
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.PossibleTitleAuthorSwap);

        await dashboard.RepairTitleAuthorCommand.ExecuteAsync(null);

        service.BookId.Should().BeNull();
        dashboard.SelectedIssue.Rows.Should().ContainSingle();
    }

    [Theory]
    [InlineData(MetadataQualityTitleAuthorRepairStatus.Failed, "MetadataQualityTitleAuthorRepairFailed")]
    [InlineData(MetadataQualityTitleAuthorRepairStatus.SavedWithWriteBackErrors, "MetadataQualityTitleAuthorRepairWriteBackWarning")]
    [InlineData(MetadataQualityTitleAuthorRepairStatus.NotApplicable, "MetadataQualityTitleAuthorRepairNotNeeded")]
    public async Task Swap_reports_the_result(
        MetadataQualityTitleAuthorRepairStatus status,
        string messageKey)
    {
        var book = CreateBook("Jan Jansen", "De verdwenen stad");
        var returnedBook = status is MetadataQualityTitleAuthorRepairStatus.SavedWithWriteBackErrors
            or MetadataQualityTitleAuthorRepairStatus.NotApplicable
                ? CreateRepairedBook(book)
                : book;
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => $"localized:{key}",
            titleAuthorRepairService: new RecordingRepairService(returnedBook, status),
            showTitleAuthorRepair: (_, _) => Task.FromResult(true));
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.PossibleTitleAuthorSwap);

        await dashboard.RepairTitleAuthorCommand.ExecuteAsync(null);

        dashboard.StatusMessage.Should().Be($"localized:{messageKey}");
    }

    private static MetadataQualityDashboardViewModel CreateDashboard(
        Book book,
        IMetadataQualityTitleAuthorRepairService service) =>
        new(
            [book],
            key => key,
            titleAuthorRepairService: service,
            showTitleAuthorRepair: (_, _) => Task.FromResult(true));

    private static Book CreateBook(string title, string author)
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata(title, [author], Language: "nl", CoverBytes: [1]),
            ReadingStatus.Unread,
            null,
            now,
            now);
    }

    private static Book CreateRepairedBook(Book book) =>
        book with
        {
            Metadata = new BookMetadata(
                book.Metadata.Authors.Single(),
                [book.Metadata.Title],
                Language: book.Metadata.Language,
                CoverBytes: book.Metadata.CoverBytes),
            UpdatedUtc = DateTimeOffset.UtcNow
        };

    private sealed class RecordingRepairService(
        Book repairedBook,
        MetadataQualityTitleAuthorRepairStatus status = MetadataQualityTitleAuthorRepairStatus.Succeeded)
        : IMetadataQualityTitleAuthorRepairService
    {
        public Guid? BookId { get; private set; }

        public Task<MetadataQualityTitleAuthorRepairResult> RepairAsync(
            Guid bookId,
            CancellationToken cancellationToken)
        {
            BookId = bookId;
            return Task.FromResult(new MetadataQualityTitleAuthorRepairResult(
                repairedBook.Id,
                status,
                repairedBook));
        }
    }
}

using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class MetadataQualityDashboardTagRepairTests
{
    [Fact]
    public void Tag_cleanup_is_enabled_only_for_a_selected_messy_tag_row()
    {
        var book = CreateBook(["Tag, Tweede"]);
        var dashboard = CreateDashboard(book, new RecordingTagRepairService(book));

        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.MessyTags);
        dashboard.RepairMessyTagsCommand.CanExecute(null).Should().BeTrue();

        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.MissingCover);
        dashboard.RepairMessyTagsCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task Tag_cleanup_shows_an_editable_proposal_and_reevaluates_the_book()
    {
        var book = CreateBook([" Thriller ", "Misdaad, Spanning", "thriller"]);
        var repairedBook = CopyWithTags(book, ["Thriller", "Detective"]);
        var service = new RecordingTagRepairService(repairedBook);
        MetadataQualityTagRepairViewModel? shownRepair = null;
        Book? notifiedBook = null;
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => key,
            tagRepairService: service,
            showTagRepair: (repair, _) =>
            {
                shownRepair = repair;
                repair.TagsText = "Thriller\r\nDetective";
                return Task.FromResult(true);
            },
            booksRepaired: repaired => notifiedBook = repaired.Single());
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.MessyTags);

        await dashboard.RepairMessyTagsCommand.ExecuteAsync(null);

        shownRepair.Should().NotBeNull();
        shownRepair!.CurrentTagsText.Should().Contain("Misdaad, Spanning");
        service.BookId.Should().Be(book.Id);
        service.Tags.Should().Equal("Thriller", "Detective");
        notifiedBook.Should().BeSameAs(repairedBook);
        dashboard.SelectedIssue.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task Tag_cleanup_does_not_write_when_the_dialog_is_cancelled()
    {
        var book = CreateBook(["Tag, Tweede"]);
        var service = new RecordingTagRepairService(book);
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => key,
            tagRepairService: service,
            showTagRepair: (_, _) => Task.FromResult(false));
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.MessyTags);

        await dashboard.RepairMessyTagsCommand.ExecuteAsync(null);

        service.BookId.Should().BeNull();
        dashboard.SelectedIssue.Rows.Should().ContainSingle();
    }

    [Theory]
    [InlineData(MetadataQualityTagRepairStatus.Failed, "MetadataQualityTagRepairFailed")]
    [InlineData(MetadataQualityTagRepairStatus.SavedWithWriteBackErrors, "MetadataQualityTagRepairWriteBackWarning")]
    [InlineData(MetadataQualityTagRepairStatus.NotApplicable, "MetadataQualityTagRepairNotNeeded")]
    public async Task Tag_cleanup_reports_the_result(
        MetadataQualityTagRepairStatus status,
        string messageKey)
    {
        var book = CreateBook(["Tag, Tweede"]);
        var returnedBook = status is MetadataQualityTagRepairStatus.SavedWithWriteBackErrors
            or MetadataQualityTagRepairStatus.NotApplicable
                ? CopyWithTags(book, ["Tag", "Tweede"])
                : book;
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => $"localized:{key}",
            tagRepairService: new RecordingTagRepairService(returnedBook, status),
            showTagRepair: (_, _) => Task.FromResult(true));
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.MessyTags);

        await dashboard.RepairMessyTagsCommand.ExecuteAsync(null);

        dashboard.StatusMessage.Should().Be($"localized:{messageKey}");
    }

    [Fact]
    public async Task Tag_cleanup_removes_a_book_that_disappeared_during_repair()
    {
        var book = CreateBook(["Tag, Tweede"]);
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => $"localized:{key}",
            tagRepairService: new RecordingTagRepairService(
                repairedBook: null,
                MetadataQualityTagRepairStatus.NotFound),
            showTagRepair: (_, _) => Task.FromResult(true));
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.MessyTags);

        await dashboard.RepairMessyTagsCommand.ExecuteAsync(null);

        dashboard.SelectedIssue.Rows.Should().BeEmpty();
        dashboard.StatusMessage.Should().Be("localized:MetadataQualityBookUnavailableMessage");
    }

    private static MetadataQualityDashboardViewModel CreateDashboard(
        Book book,
        IMetadataQualityTagRepairService service) =>
        new(
            [book],
            key => key,
            tagRepairService: service,
            showTagRepair: (_, _) => Task.FromResult(true));

    private static Book CreateBook(IReadOnlyList<string> tags)
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata("Boek", ["Auteur"], Language: "nl", Tags: tags),
            ReadingStatus.Unread,
            null,
            now,
            now);
    }

    private static Book CopyWithTags(Book book, IReadOnlyList<string> tags) =>
        book with
        {
            Metadata = new BookMetadata(
                book.Metadata.Title,
                book.Metadata.Authors,
                book.Metadata.Description,
                book.Metadata.Language,
                book.Metadata.Publisher,
                book.Metadata.PublicationDate,
                tags,
                book.Metadata.Series,
                book.Metadata.SeriesNumber,
                book.Metadata.Isbn,
                book.Metadata.CoverBytes),
            UpdatedUtc = DateTimeOffset.UtcNow
        };

    private sealed class RecordingTagRepairService(
        Book? repairedBook,
        MetadataQualityTagRepairStatus status = MetadataQualityTagRepairStatus.Succeeded)
        : IMetadataQualityTagRepairService
    {
        public Guid? BookId { get; private set; }
        public IReadOnlyList<string> Tags { get; private set; } = [];

        public Task<MetadataQualityTagRepairResult> RepairAsync(
            Guid bookId,
            IReadOnlyList<string> tags,
            CancellationToken cancellationToken)
        {
            BookId = bookId;
            Tags = tags.ToArray();
            return Task.FromResult(new MetadataQualityTagRepairResult(
                bookId,
                status,
                repairedBook));
        }
    }
}

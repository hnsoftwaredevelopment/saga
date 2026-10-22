using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class MetadataQualityDashboardTitleAuthorRepairTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Bulk_swap_writes_only_checked_books_after_confirmation(bool confirm, bool select)
    {
        var first = CreateBook("Jan Jansen", "De verdwenen stad");
        var second = CreateBook("Piet Peters", "Het verloren boek");
        var service = new RecordingRepairService(CreateRepairedBook(first));
        var notifications = new List<IReadOnlyList<Book>>();
        var dashboard = new MetadataQualityDashboardViewModel([first, second],
            key => key == "MetadataQualityTitleAuthorRepairBulkSucceeded" ? "{0} changed" : key,
            titleAuthorRepairService: service,
            showTitleAuthorRepair: (repair, _) =>
            {
                repair.Previews.Should().HaveCount(2);
                repair.Previews.Should().OnlyContain(row => !row.IsSelected);
                repair.CanConfirm.Should().BeFalse();
                if (select) { repair.Previews.Single(row => row.BookId == first.Id).IsSelected = true; }
                return Task.FromResult(confirm);
            }, booksRepaired: notifications.Add);
        dashboard.SelectedIssue = dashboard.Issues.Single(issue => issue.SignalKey == MetadataQualitySignalKeys.PossibleTitleAuthorSwap);
        dashboard.SetSelectedBooks(dashboard.SelectedIssue.Rows);
        dashboard.RepairTitleAuthorCommand.CanExecute(null).Should().BeTrue();

        await dashboard.RepairTitleAuthorCommand.ExecuteAsync(null);

        if (confirm && select)
        {
            service.Calls.Should().ContainSingle().Which.Should().Be(first.Id);
            service.Expected!.Title.Should().Be(first.Metadata.Title);
            service.Expected.Authors.Should().Equal(first.Metadata.Authors);
            notifications.Should().ContainSingle();
            dashboard.SelectedIssue.Rows.Should().ContainSingle(row => row.Id == second.Id);
            dashboard.StatusMessage.Should().Be("1 changed");
            dashboard.IsStatusMessageSuccess.Should().BeTrue();
        }
        else
        {
            service.Calls.Should().BeEmpty();
            notifications.Should().BeEmpty();
            dashboard.SelectedIssue.Rows.Should().HaveCount(2);
        }
    }

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
            booksRepaired: repaired => notifiedBook = repaired.Single());
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

    [Fact]
    public async Task Bulk_swap_reconciles_mixed_results_and_continues_after_individual_errors()
    {
        var failed = CreateBook("Jan Jansen", "De verdwenen stad");
        var succeeded = CreateBook("Piet Peters", "Het verloren boek");
        var warning = CreateBook("Kees Bakker", "De grote reis");
        var changed = CreateBook("Mark Visser", "Een mooie dag");
        var gone = CreateBook("Henk Bos", "Het verre land");
        var current = CreateBook("Mark Visser", "Een ander verhaal") with { Id = changed.Id };
        var service = new RecordingRepairService(
            new(succeeded.Id, MetadataQualityTitleAuthorRepairStatus.Succeeded, CreateRepairedBook(succeeded)),
            new(warning.Id, MetadataQualityTitleAuthorRepairStatus.SavedWithWriteBackErrors, CreateRepairedBook(warning)),
            new(changed.Id, MetadataQualityTitleAuthorRepairStatus.NotApplicable, current),
            new(gone.Id, MetadataQualityTitleAuthorRepairStatus.NotFound)) { ThrowFor = failed.Id };
        var notifications = new List<IReadOnlyList<Book>>();
        var dashboard = new MetadataQualityDashboardViewModel([failed, succeeded, warning, changed, gone],
            key => key == "MetadataQualityTitleAuthorRepairBulkResult" ? "{0}:{1}:{2}:{3}:{4}" : key,
            titleAuthorRepairService: service,
            showTitleAuthorRepair: (repair, _) =>
            {
                foreach (var row in repair.Previews) { row.IsSelected = true; }
                return Task.FromResult(true);
            }, booksRepaired: notifications.Add);
        dashboard.SelectedIssue = dashboard.Issues.Single(issue => issue.SignalKey == MetadataQualitySignalKeys.PossibleTitleAuthorSwap);
        dashboard.SetSelectedBooks(dashboard.SelectedIssue.Rows);

        await dashboard.RepairTitleAuthorCommand.ExecuteAsync(null);

        service.Calls.Should().HaveCount(5);
        dashboard.StatusMessage.Should().Be("1:1:1:1:1");
        dashboard.IsStatusMessageSuccess.Should().BeFalse();
        notifications.Should().ContainSingle().Which.Should().HaveCount(3);
        dashboard.SelectedIssue.Rows.Select(row => row.Id).Should().BeEquivalentTo([failed.Id, changed.Id]);
        dashboard.SelectedBook!.Id.Should().Be(failed.Id);
    }

    [Fact]
    public async Task Bulk_swap_keeps_completed_results_when_cancelled_during_processing()
    {
        var first = CreateBook("Jan Jansen", "De verdwenen stad");
        var second = CreateBook("Piet Peters", "Het verloren boek");
        var service = new RecordingRepairService(CreateRepairedBook(first)) { CancelFor = second.Id };
        var dashboard = new MetadataQualityDashboardViewModel([first, second], key => key,
            titleAuthorRepairService: service, showTitleAuthorRepair: (repair, _) =>
            {
                foreach (var row in repair.Previews) { row.IsSelected = true; }
                return Task.FromResult(true);
            });
        dashboard.SelectedIssue = dashboard.Issues.Single(issue => issue.SignalKey == MetadataQualitySignalKeys.PossibleTitleAuthorSwap);
        dashboard.SetSelectedBooks(dashboard.SelectedIssue.Rows);

        var action = () => dashboard.RepairTitleAuthorCommand.ExecuteAsync(null);
        await action.Should().ThrowAsync<OperationCanceledException>();

        dashboard.SelectedIssue.Rows.Should().ContainSingle(row => row.Id == second.Id);
        dashboard.SelectedBook!.Id.Should().Be(second.Id);
    }

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

    private sealed class RecordingRepairService(params MetadataQualityTitleAuthorRepairResult[] results)
        : IMetadataQualityTitleAuthorRepairService
    {
        public RecordingRepairService(Book repairedBook,
            MetadataQualityTitleAuthorRepairStatus status = MetadataQualityTitleAuthorRepairStatus.Succeeded)
            : this(new MetadataQualityTitleAuthorRepairResult(repairedBook.Id, status, repairedBook)) { }

        public Guid? BookId { get; private set; }
        public List<Guid> Calls { get; } = [];
        public MetadataQualityTitleAuthorSnapshot? Expected { get; private set; }
        public Guid? ThrowFor { get; set; }
        public Guid? CancelFor { get; set; }

        public Task<MetadataQualityTitleAuthorRepairResult> RepairAsync(
            Guid bookId,
            CancellationToken cancellationToken,
            MetadataQualityTitleAuthorSnapshot? expected = null)
        {
            BookId = bookId;
            Calls.Add(bookId);
            Expected = expected;
            if (ThrowFor == bookId) { throw new IOException("Unavailable"); }
            if (CancelFor == bookId) { throw new OperationCanceledException(); }
            return Task.FromResult(results.Single(item => item.BookId == bookId));
        }
    }
}

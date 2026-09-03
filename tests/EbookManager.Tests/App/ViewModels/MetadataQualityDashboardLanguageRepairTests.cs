using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class MetadataQualityDashboardLanguageRepairTests
{
    [Fact]
    public void Language_repair_is_enabled_only_for_the_unknown_language_signal()
    {
        var book = CreateBook(language: null);
        var dashboard = CreateDashboard(book, new RecordingLanguageRepairService(book));

        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.UnknownLanguage);

        dashboard.RepairUnknownLanguageCommand.CanExecute(null).Should().BeTrue();

        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.MissingCover);

        dashboard.RepairUnknownLanguageCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task Repair_unknown_language_saves_selection_and_reevaluates_the_book()
    {
        var book = CreateBook("fictional-language");
        var repairedBook = book with
        {
            Metadata = CopyMetadataWithLanguage(book.Metadata, "nl")
        };
        var service = new RecordingLanguageRepairService(repairedBook);
        MetadataQualityLanguageRepairViewModel? shownRepair = null;
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => key,
            languageRepairService: service,
            showLanguageRepair: (repair, _) =>
            {
                shownRepair = repair;
                repair.SelectedLanguage = repair.Languages.Single(option => option.Code == "nl");
                return Task.FromResult(true);
            });
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.UnknownLanguage);

        await dashboard.RepairUnknownLanguageCommand.ExecuteAsync(null);

        shownRepair.Should().NotBeNull();
        service.BookIds.Should().Equal(book.Id);
        service.Language.Should().Be("nl");
        dashboard.SelectedIssue.Rows.Should().BeEmpty();
        dashboard.SelectedBook.Should().BeNull();
    }

    [Fact]
    public async Task Repair_unknown_language_does_not_write_when_dialog_is_cancelled()
    {
        var book = CreateBook(language: null);
        var service = new RecordingLanguageRepairService(book);
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => key,
            languageRepairService: service,
            showLanguageRepair: (_, _) => Task.FromResult(false));
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.UnknownLanguage);

        await dashboard.RepairUnknownLanguageCommand.ExecuteAsync(null);

        service.BookIds.Should().BeEmpty();
        dashboard.SelectedIssue.Rows.Should().ContainSingle();
    }

    [Fact]
    public async Task Repair_unknown_language_keeps_row_and_sets_status_when_save_fails()
    {
        var book = CreateBook(language: null);
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => $"localized:{key}",
            languageRepairService: new RecordingLanguageRepairService(
                book,
                MetadataQualityLanguageRepairStatus.Failed),
            showLanguageRepair: SelectDutchLanguage);
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.UnknownLanguage);

        await dashboard.RepairUnknownLanguageCommand.ExecuteAsync(null);

        dashboard.SelectedIssue.Rows.Should().ContainSingle();
        dashboard.StatusMessage.Should().Be("localized:MetadataQualityLanguageRepairFailed");
    }

    [Fact]
    public async Task Repair_unknown_language_shows_warning_after_a_partial_writeback_failure()
    {
        var book = CreateBook(language: null);
        var repairedBook = book with
        {
            Metadata = CopyMetadataWithLanguage(book.Metadata, "nl")
        };
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => $"localized:{key}",
            languageRepairService: new RecordingLanguageRepairService(
                repairedBook,
                MetadataQualityLanguageRepairStatus.SavedWithWriteBackErrors),
            showLanguageRepair: SelectDutchLanguage);
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.UnknownLanguage);

        await dashboard.RepairUnknownLanguageCommand.ExecuteAsync(null);

        dashboard.SelectedIssue.Rows.Should().BeEmpty();
        dashboard.StatusMessage.Should().Be("localized:MetadataQualityLanguageRepairWriteBackWarning");
    }

    [Fact]
    public async Task Repair_unknown_language_reports_neutrally_when_language_is_already_valid()
    {
        var staleBook = CreateBook(language: null);
        var currentBook = staleBook with
        {
            Metadata = CopyMetadataWithLanguage(staleBook.Metadata, "en")
        };
        var dashboard = new MetadataQualityDashboardViewModel(
            [staleBook],
            key => $"localized:{key}",
            languageRepairService: new RecordingLanguageRepairService(
                currentBook,
                MetadataQualityLanguageRepairStatus.NotApplicable),
            showLanguageRepair: SelectDutchLanguage);
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.UnknownLanguage);

        await dashboard.RepairUnknownLanguageCommand.ExecuteAsync(null);

        dashboard.SelectedIssue.Rows.Should().BeEmpty();
        dashboard.StatusMessage.Should().Be("localized:MetadataQualityLanguageRepairNotNeeded");
    }

    private static Task<bool> SelectDutchLanguage(
        MetadataQualityLanguageRepairViewModel repair,
        CancellationToken cancellationToken)
    {
        repair.SelectedLanguage = repair.Languages.Single(option => option.Code == "nl");
        return Task.FromResult(true);
    }

    private static MetadataQualityDashboardViewModel CreateDashboard(
        Book book,
        IMetadataQualityLanguageRepairService service) =>
        new(
            [book],
            key => key,
            languageRepairService: service,
            showLanguageRepair: (_, _) => Task.FromResult(true));

    private static Book CreateBook(string? language)
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata("Boek", ["Auteur"], Language: language),
            ReadingStatus.Unread,
            null,
            now,
            now);
    }

    private static BookMetadata CopyMetadataWithLanguage(BookMetadata metadata, string language) =>
        new(
            metadata.Title,
            metadata.Authors,
            metadata.Description,
            language,
            metadata.Publisher,
            metadata.PublicationDate,
            metadata.Tags,
            metadata.Series,
            metadata.SeriesNumber,
            metadata.Isbn,
            metadata.CoverBytes);

    private sealed class RecordingLanguageRepairService(
        Book repairedBook,
        MetadataQualityLanguageRepairStatus status = MetadataQualityLanguageRepairStatus.Succeeded)
        : IMetadataQualityLanguageRepairService
    {
        public IReadOnlyList<Guid> BookIds { get; private set; } = [];
        public string? Language { get; private set; }

        public Task<MetadataQualityLanguageRepairBatchResult> RepairAsync(
            IReadOnlyCollection<Guid> bookIds,
            string language,
            CancellationToken cancellationToken)
        {
            BookIds = bookIds.ToArray();
            Language = language;
            return Task.FromResult(new MetadataQualityLanguageRepairBatchResult(
            [
                new MetadataQualityLanguageRepairItemResult(
                    repairedBook.Id,
                    status,
                    repairedBook)
            ]));
        }
    }
}

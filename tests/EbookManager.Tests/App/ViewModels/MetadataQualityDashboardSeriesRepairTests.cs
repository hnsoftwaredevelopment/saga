using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class MetadataQualityDashboardSeriesRepairTests
{
    [Fact]
    public void Series_repair_is_enabled_only_for_series_number_without_series()
    {
        var book = CreateBook(series: null, seriesNumber: 2);
        var dashboard = CreateDashboard(book, new RecordingSeriesRepairService(book));

        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.SeriesNumberWithoutSeries);
        dashboard.RepairMissingSeriesCommand.CanExecute(null).Should().BeTrue();

        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.MissingCover);
        dashboard.RepairMissingSeriesCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task Repair_missing_series_uses_known_suggestions_and_reevaluates_the_book()
    {
        var book = CreateBook(series: null, seriesNumber: 2);
        var knownSeriesBook = CreateBook(series: "Discworld", seriesNumber: 1);
        var repairedBook = book with { Metadata = CopyMetadataWithSeries(book.Metadata, "Discworld") };
        var service = new RecordingSeriesRepairService(repairedBook);
        MetadataQualitySeriesRepairViewModel? shownRepair = null;
        var dashboard = new MetadataQualityDashboardViewModel(
            [book, knownSeriesBook],
            key => key,
            seriesRepairService: service,
            showSeriesRepair: (repair, _) =>
            {
                shownRepair = repair;
                repair.UseSuggestion(repair.Suggestions.Single());
                return Task.FromResult(true);
            });
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.SeriesNumberWithoutSeries);

        await dashboard.RepairMissingSeriesCommand.ExecuteAsync(null);

        shownRepair.Should().NotBeNull();
        shownRepair!.SeriesNumber.Should().Be(2);
        service.BookIds.Should().Equal(book.Id);
        service.Series.Should().Be("Discworld");
        dashboard.SelectedIssue.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task Repair_missing_series_does_not_write_when_dialog_is_cancelled()
    {
        var book = CreateBook(series: null, seriesNumber: 1);
        var service = new RecordingSeriesRepairService(book);
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => key,
            seriesRepairService: service,
            showSeriesRepair: (_, _) => Task.FromResult(false));
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.SeriesNumberWithoutSeries);

        await dashboard.RepairMissingSeriesCommand.ExecuteAsync(null);

        service.BookIds.Should().BeEmpty();
        dashboard.SelectedIssue.Rows.Should().ContainSingle();
    }

    [Theory]
    [InlineData(MetadataQualitySeriesRepairStatus.Failed, "MetadataQualitySeriesRepairFailed")]
    [InlineData(MetadataQualitySeriesRepairStatus.SavedWithWriteBackErrors, "MetadataQualitySeriesRepairWriteBackWarning")]
    [InlineData(MetadataQualitySeriesRepairStatus.NotApplicable, "MetadataQualitySeriesRepairNotNeeded")]
    public async Task Repair_missing_series_reports_the_result(
        MetadataQualitySeriesRepairStatus status,
        string messageKey)
    {
        var book = CreateBook(series: null, seriesNumber: 1);
        var returnedBook = status is MetadataQualitySeriesRepairStatus.SavedWithWriteBackErrors
            or MetadataQualitySeriesRepairStatus.NotApplicable
                ? book with { Metadata = CopyMetadataWithSeries(book.Metadata, "Serie") }
                : book;
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => $"localized:{key}",
            seriesRepairService: new RecordingSeriesRepairService(returnedBook, status),
            showSeriesRepair: (repair, _) =>
            {
                repair.SeriesText = "Serie";
                return Task.FromResult(true);
            });
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.SeriesNumberWithoutSeries);

        await dashboard.RepairMissingSeriesCommand.ExecuteAsync(null);

        dashboard.StatusMessage.Should().Be($"localized:{messageKey}");
    }

    private static MetadataQualityDashboardViewModel CreateDashboard(
        Book book,
        IMetadataQualitySeriesRepairService service) =>
        new(
            [book],
            key => key,
            seriesRepairService: service,
            showSeriesRepair: (_, _) => Task.FromResult(true));

    private static Book CreateBook(string? series, decimal? seriesNumber)
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata(
                "Boek",
                ["Auteur"],
                Language: "nl",
                Series: series,
                SeriesNumber: seriesNumber,
                CoverBytes: [1]),
            ReadingStatus.Unread,
            null,
            now,
            now);
    }

    private static BookMetadata CopyMetadataWithSeries(BookMetadata metadata, string series) =>
        new(
            metadata.Title,
            metadata.Authors,
            metadata.Description,
            metadata.Language,
            metadata.Publisher,
            metadata.PublicationDate,
            metadata.Tags,
            series,
            metadata.SeriesNumber,
            metadata.Isbn,
            metadata.CoverBytes);

    private sealed class RecordingSeriesRepairService(
        Book repairedBook,
        MetadataQualitySeriesRepairStatus status = MetadataQualitySeriesRepairStatus.Succeeded)
        : IMetadataQualitySeriesRepairService
    {
        public IReadOnlyList<Guid> BookIds { get; private set; } = [];
        public string? Series { get; private set; }

        public Task<MetadataQualitySeriesRepairBatchResult> RepairAsync(
            IReadOnlyCollection<Guid> bookIds,
            string series,
            CancellationToken cancellationToken)
        {
            BookIds = bookIds.ToArray();
            Series = series;
            return Task.FromResult(new MetadataQualitySeriesRepairBatchResult(
            [
                new MetadataQualitySeriesRepairItemResult(repairedBook.Id, status, repairedBook)
            ]));
        }
    }
}

using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class MetadataQualityDashboardCoverRepairTests
{
    [Fact]
    public void Cover_search_is_enabled_only_for_a_selected_book_without_a_cover()
    {
        var book = CreateBook();
        var dashboard = CreateDashboard(book);

        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.MissingCover);
        dashboard.SearchCoverCommand.CanExecute(null).Should().BeTrue();

        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.UnknownLanguage);
        dashboard.SearchCoverCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task Cover_search_downloads_the_choice_repairs_the_book_and_reevaluates_it()
    {
        var book = CreateBook();
        var candidate = Candidate();
        var bytes = new byte[] { 1, 2, 3, 4 };
        var repairedBook = book with
        {
            Metadata = CopyMetadataWithCover(book.Metadata, bytes),
            CoverRelativePath = $"books/{book.Id:N}/cover.jpg"
        };
        var searchService = new RecordingCoverSearchService(candidate, bytes);
        var repairService = new RecordingCoverRepairService(repairedBook);
        Book? notifiedBook = null;
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => key,
            coverSearchService: searchService,
            showCoverSearch: async (search, cancellationToken) =>
            {
                await search.LoadAsync(cancellationToken);
                search.SelectedCandidate = search.Candidates.Single();
                return true;
            },
            coverRepairService: repairService,
            bookRepaired: repaired => notifiedBook = repaired);
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.MissingCover);

        await dashboard.SearchCoverCommand.ExecuteAsync(null);

        searchService.SearchQuery.Should().Be(new BookCoverSearchQuery(
            book.Metadata.Title,
            book.Metadata.Authors,
            book.Metadata.Isbn));
        searchService.DownloadedCandidateId.Should().Be(candidate.CandidateId);
        repairService.BookId.Should().Be(book.Id);
        repairService.CoverBytes.Should().Equal(bytes);
        notifiedBook.Should().BeSameAs(repairedBook);
        dashboard.SelectedIssue.Rows.Should().BeEmpty();
        dashboard.StatusMessage.Should().BeNull();
    }

    [Fact]
    public async Task Cover_search_does_not_download_when_the_dialog_is_cancelled()
    {
        var book = CreateBook();
        var searchService = new RecordingCoverSearchService(Candidate(), new byte[] { 1 });
        var repairService = new RecordingCoverRepairService(book);
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => key,
            coverSearchService: searchService,
            showCoverSearch: (_, _) => Task.FromResult(false),
            coverRepairService: repairService);
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.MissingCover);

        await dashboard.SearchCoverCommand.ExecuteAsync(null);

        searchService.DownloadedCandidateId.Should().BeNull();
        repairService.BookId.Should().BeNull();
        dashboard.SelectedIssue.Rows.Should().ContainSingle();
    }

    [Fact]
    public async Task Cover_search_reports_a_failed_final_download()
    {
        var book = CreateBook();
        var searchService = new RecordingCoverSearchService(
            Candidate(),
            downloadResult: new(BookCoverDownloadStatus.Failed));
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => $"localized:{key}",
            coverSearchService: searchService,
            showCoverSearch: async (search, cancellationToken) =>
            {
                await search.LoadAsync(cancellationToken);
                search.SelectedCandidate = search.Candidates.Single();
                return true;
            },
            coverRepairService: new RecordingCoverRepairService(book));
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.MissingCover);

        await dashboard.SearchCoverCommand.ExecuteAsync(null);

        dashboard.StatusMessage.Should().Be("localized:MetadataQualityCoverDownloadFailed");
    }

    [Theory]
    [InlineData(MetadataQualityCoverRepairStatus.Failed, "MetadataQualityCoverRepairFailed")]
    [InlineData(MetadataQualityCoverRepairStatus.SavedWithWriteBackErrors, "MetadataQualityCoverRepairWriteBackWarning")]
    [InlineData(MetadataQualityCoverRepairStatus.NotApplicable, "MetadataQualityCoverRepairNotNeeded")]
    public async Task Cover_search_reports_the_repair_result(
        MetadataQualityCoverRepairStatus status,
        string messageKey)
    {
        var book = CreateBook();
        var candidate = Candidate();
        var bytes = new byte[] { 1, 2, 3 };
        var returnedBook = status is MetadataQualityCoverRepairStatus.SavedWithWriteBackErrors
            or MetadataQualityCoverRepairStatus.NotApplicable
                ? book with { Metadata = CopyMetadataWithCover(book.Metadata, bytes) }
                : book;
        var dashboard = new MetadataQualityDashboardViewModel(
            [book],
            key => $"localized:{key}",
            coverSearchService: new RecordingCoverSearchService(candidate, bytes),
            showCoverSearch: async (search, cancellationToken) =>
            {
                await search.LoadAsync(cancellationToken);
                search.SelectedCandidate = search.Candidates.Single();
                return true;
            },
            coverRepairService: new RecordingCoverRepairService(returnedBook, status));
        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.SignalKey == MetadataQualitySignalKeys.MissingCover);

        await dashboard.SearchCoverCommand.ExecuteAsync(null);

        dashboard.StatusMessage.Should().Be($"localized:{messageKey}");
    }

    private static MetadataQualityDashboardViewModel CreateDashboard(Book book) =>
        new(
            [book],
            key => key,
            coverSearchService: new RecordingCoverSearchService(Candidate(), new byte[] { 1 }),
            showCoverSearch: (_, _) => Task.FromResult(false),
            coverRepairService: new RecordingCoverRepairService(book));

    private static Book CreateBook()
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata(
                "The book",
                ["The author"],
                Language: "nl",
                Isbn: "9781234567890"),
            ReadingStatus.Unread,
            null,
            now,
            now);
    }

    private static BookCoverCandidate Candidate() =>
        new(42, "Open Library", "The book", ["The author"], [0xFF, 0xD8, 0xFF, 0xD9], 400, 600);

    private static BookMetadata CopyMetadataWithCover(BookMetadata metadata, byte[] coverBytes) =>
        new(
            metadata.Title,
            metadata.Authors,
            metadata.Description,
            metadata.Language,
            metadata.Publisher,
            metadata.PublicationDate,
            metadata.Tags,
            metadata.Series,
            metadata.SeriesNumber,
            metadata.Isbn,
            coverBytes);

    private sealed class RecordingCoverSearchService : IBookCoverSearchService
    {
        private readonly BookCoverCandidate candidate;
        private readonly BookCoverDownloadResult downloadResult;

        public RecordingCoverSearchService(BookCoverCandidate candidate, byte[] bytes)
            : this(candidate, new BookCoverDownloadResult(
                BookCoverDownloadStatus.Succeeded,
                bytes,
                400,
                600))
        {
        }

        public RecordingCoverSearchService(
            BookCoverCandidate candidate,
            BookCoverDownloadResult downloadResult)
        {
            this.candidate = candidate;
            this.downloadResult = downloadResult;
        }

        public BookCoverSearchQuery? SearchQuery { get; private set; }
        public string? DownloadedCandidateId { get; private set; }

        public Task<BookCoverSearchResult> SearchAsync(
            BookCoverSearchQuery query,
            CancellationToken cancellationToken)
        {
            SearchQuery = query;
            return Task.FromResult(new BookCoverSearchResult(
                BookCoverSearchStatus.Succeeded,
                [candidate]));
        }

        public Task<BookCoverDownloadResult> DownloadAsync(
            string candidateId,
            CancellationToken cancellationToken)
        {
            DownloadedCandidateId = candidateId;
            return Task.FromResult(downloadResult);
        }
    }

    private sealed class RecordingCoverRepairService(
        Book repairedBook,
        MetadataQualityCoverRepairStatus status = MetadataQualityCoverRepairStatus.Succeeded)
        : IMetadataQualityCoverRepairService
    {
        public Guid? BookId { get; private set; }
        public byte[]? CoverBytes { get; private set; }

        public Task<MetadataQualityCoverRepairResult> RepairAsync(
            Guid bookId,
            byte[] coverBytes,
            CancellationToken cancellationToken)
        {
            BookId = bookId;
            CoverBytes = coverBytes;
            return Task.FromResult(new MetadataQualityCoverRepairResult(
                bookId,
                status,
                repairedBook));
        }
    }
}

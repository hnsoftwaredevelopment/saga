using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;

namespace EbookManager.Application.Metadata;

public interface IMetadataQualitySeriesRepairService
{
    Task<MetadataQualitySeriesRepairBatchResult> RepairAsync(
        IReadOnlyCollection<Guid> bookIds,
        string series,
        CancellationToken cancellationToken);
}

public sealed class MetadataQualitySeriesRepairService(
    IBookRepository bookRepository,
    BookService bookService) : IMetadataQualitySeriesRepairService
{
    private readonly IBookRepository bookRepository = bookRepository;
    private readonly BookService bookService = bookService;

    public async Task<MetadataQualitySeriesRepairBatchResult> RepairAsync(
        IReadOnlyCollection<Guid> bookIds,
        string series,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bookIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(series);

        var normalizedSeries = series.Trim();
        var distinctBookIds = bookIds.Distinct().ToArray();
        if (distinctBookIds.Length == 0 || distinctBookIds.Any(id => id == Guid.Empty))
        {
            throw new ArgumentException("At least one valid book id is required.", nameof(bookIds));
        }

        var results = new List<MetadataQualitySeriesRepairItemResult>(distinctBookIds.Length);
        foreach (var bookId in distinctBookIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentBook = await bookRepository.GetAsync(bookId, cancellationToken);
            if (currentBook is null)
            {
                results.Add(new(bookId, MetadataQualitySeriesRepairStatus.NotFound));
                continue;
            }

            if (!MetadataQualitySignalEvaluator.Applies(
                    currentBook,
                    MetadataQualitySignalKeys.SeriesNumberWithoutSeries))
            {
                results.Add(new(bookId, MetadataQualitySeriesRepairStatus.NotApplicable, currentBook));
                continue;
            }

            var updatedBook = currentBook with
            {
                Metadata = CopyMetadataWithSeries(currentBook.Metadata, normalizedSeries),
                UpdatedUtc = DateTimeOffset.UtcNow
            };
            var saveResult = await bookService.SaveAsync(updatedBook, cancellationToken);
            var reloadedBook = await bookRepository.GetAsync(bookId, cancellationToken);
            var databaseWasUpdated = reloadedBook?.Metadata.Series == normalizedSeries;
            var hasFileWriteBackError = saveResult.FileResults.Any(file =>
                file.Result.Status == MetadataWriteBackStatus.Failed);
            var status = saveResult.Status switch
            {
                BookSaveStatus.Succeeded when hasFileWriteBackError =>
                    MetadataQualitySeriesRepairStatus.SavedWithWriteBackErrors,
                BookSaveStatus.Succeeded => MetadataQualitySeriesRepairStatus.Succeeded,
                BookSaveStatus.Failed when databaseWasUpdated =>
                    MetadataQualitySeriesRepairStatus.SavedWithWriteBackErrors,
                _ => MetadataQualitySeriesRepairStatus.Failed
            };
            results.Add(new(
                bookId,
                status,
                reloadedBook,
                saveResult.Message,
                saveResult.FileResults));
        }

        return new MetadataQualitySeriesRepairBatchResult(results.AsReadOnly());
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
}

public sealed record MetadataQualitySeriesRepairBatchResult(
    IReadOnlyList<MetadataQualitySeriesRepairItemResult> Items);

public sealed record MetadataQualitySeriesRepairItemResult(
    Guid BookId,
    MetadataQualitySeriesRepairStatus Status,
    Book? Book = null,
    string? Message = null,
    IReadOnlyList<BookFileWriteBackResult>? FileResults = null);

public enum MetadataQualitySeriesRepairStatus
{
    Succeeded,
    SavedWithWriteBackErrors,
    NotFound,
    NotApplicable,
    Failed
}

using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;

namespace EbookManager.Application.Metadata;

public interface IMetadataQualityAuthorRepairService
{
    Task<MetadataQualityAuthorRepairBatchResult> RepairAsync(
        IReadOnlyCollection<Guid> bookIds,
        string author,
        CancellationToken cancellationToken);
}

public sealed class MetadataQualityAuthorRepairService(
    IBookRepository bookRepository,
    BookService bookService) : IMetadataQualityAuthorRepairService
{
    private readonly IBookRepository bookRepository = bookRepository;
    private readonly BookService bookService = bookService;

    public async Task<MetadataQualityAuthorRepairBatchResult> RepairAsync(
        IReadOnlyCollection<Guid> bookIds,
        string author,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bookIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(author);

        var normalizedAuthor = author.Trim();
        if (normalizedAuthor.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("A usable author is required.", nameof(author));
        }

        var distinctBookIds = bookIds.Distinct().ToArray();
        if (distinctBookIds.Length == 0 || distinctBookIds.Any(id => id == Guid.Empty))
        {
            throw new ArgumentException("At least one valid book id is required.", nameof(bookIds));
        }

        var results = new List<MetadataQualityAuthorRepairItemResult>(distinctBookIds.Length);
        foreach (var bookId in distinctBookIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentBook = await bookRepository.GetAsync(bookId, cancellationToken);
            if (currentBook is null)
            {
                results.Add(new MetadataQualityAuthorRepairItemResult(
                    bookId,
                    MetadataQualityAuthorRepairStatus.NotFound));
                continue;
            }

            if (!MetadataQualitySignalEvaluator.Applies(currentBook, MetadataQualitySignalKeys.MissingAuthor))
            {
                results.Add(new MetadataQualityAuthorRepairItemResult(
                    bookId,
                    MetadataQualityAuthorRepairStatus.NotApplicable,
                    currentBook));
                continue;
            }

            var updatedBook = currentBook with
            {
                Metadata = CopyMetadataWithAuthor(currentBook.Metadata, normalizedAuthor),
                UpdatedUtc = DateTimeOffset.UtcNow
            };
            var saveResult = await bookService.SaveAsync(updatedBook, cancellationToken);
            var reloadedBook = await bookRepository.GetAsync(bookId, cancellationToken);
            results.Add(new MetadataQualityAuthorRepairItemResult(
                bookId,
                saveResult.Status == BookSaveStatus.Succeeded
                    ? MetadataQualityAuthorRepairStatus.Succeeded
                    : MetadataQualityAuthorRepairStatus.Failed,
                reloadedBook,
                saveResult.Message,
                saveResult.FileResults));
        }

        return new MetadataQualityAuthorRepairBatchResult(results.AsReadOnly());
    }

    private static BookMetadata CopyMetadataWithAuthor(BookMetadata metadata, string author) =>
        new(
            metadata.Title,
            [author],
            metadata.Description,
            metadata.Language,
            metadata.Publisher,
            metadata.PublicationDate,
            metadata.Tags,
            metadata.Series,
            metadata.SeriesNumber,
            metadata.Isbn,
            metadata.CoverBytes);
}

public sealed record MetadataQualityAuthorRepairBatchResult(
    IReadOnlyList<MetadataQualityAuthorRepairItemResult> Items);

public sealed record MetadataQualityAuthorRepairItemResult(
    Guid BookId,
    MetadataQualityAuthorRepairStatus Status,
    Book? Book = null,
    string? Message = null,
    IReadOnlyList<BookFileWriteBackResult>? FileResults = null);

public enum MetadataQualityAuthorRepairStatus
{
    Succeeded,
    NotFound,
    NotApplicable,
    Failed
}

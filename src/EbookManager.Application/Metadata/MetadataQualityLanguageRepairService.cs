using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;

namespace EbookManager.Application.Metadata;

public interface IMetadataQualityLanguageRepairService
{
    Task<MetadataQualityLanguageRepairBatchResult> RepairAsync(
        IReadOnlyCollection<Guid> bookIds,
        string language,
        CancellationToken cancellationToken);
}

public sealed class MetadataQualityLanguageRepairService(
    IBookRepository bookRepository,
    BookService bookService) : IMetadataQualityLanguageRepairService
{
    private readonly IBookRepository bookRepository = bookRepository;
    private readonly BookService bookService = bookService;

    public async Task<MetadataQualityLanguageRepairBatchResult> RepairAsync(
        IReadOnlyCollection<Guid> bookIds,
        string language,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bookIds);
        var normalizedLanguage = MetadataQualityLanguageRules.Normalize(language) ??
            throw new ArgumentException("A valid language is required.", nameof(language));
        var distinctBookIds = bookIds.Distinct().ToArray();
        if (distinctBookIds.Length == 0 || distinctBookIds.Any(id => id == Guid.Empty))
        {
            throw new ArgumentException("At least one valid book id is required.", nameof(bookIds));
        }

        var results = new List<MetadataQualityLanguageRepairItemResult>(distinctBookIds.Length);
        foreach (var bookId in distinctBookIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentBook = await bookRepository.GetAsync(bookId, cancellationToken);
            if (currentBook is null)
            {
                results.Add(new(bookId, MetadataQualityLanguageRepairStatus.NotFound));
                continue;
            }

            if (!MetadataQualitySignalEvaluator.Applies(currentBook, MetadataQualitySignalKeys.UnknownLanguage))
            {
                results.Add(new(bookId, MetadataQualityLanguageRepairStatus.NotApplicable, currentBook));
                continue;
            }

            var updatedBook = currentBook with
            {
                Metadata = CopyMetadataWithLanguage(currentBook.Metadata, normalizedLanguage),
                UpdatedUtc = DateTimeOffset.UtcNow
            };
            var saveResult = await bookService.SaveAsync(updatedBook, cancellationToken);
            var reloadedBook = await bookRepository.GetAsync(bookId, cancellationToken);
            var databaseWasUpdated = reloadedBook?.Metadata.Language == normalizedLanguage;
            var hasFileWriteBackError = saveResult.FileResults.Any(file =>
                file.Result.Status == MetadataWriteBackStatus.Failed);
            var status = saveResult.Status switch
            {
                BookSaveStatus.Succeeded when hasFileWriteBackError =>
                    MetadataQualityLanguageRepairStatus.SavedWithWriteBackErrors,
                BookSaveStatus.Succeeded => MetadataQualityLanguageRepairStatus.Succeeded,
                BookSaveStatus.Failed when databaseWasUpdated =>
                    MetadataQualityLanguageRepairStatus.SavedWithWriteBackErrors,
                _ => MetadataQualityLanguageRepairStatus.Failed
            };
            results.Add(new(
                bookId,
                status,
                reloadedBook,
                saveResult.Message,
                saveResult.FileResults));
        }

        return new MetadataQualityLanguageRepairBatchResult(results.AsReadOnly());
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
}

public sealed record MetadataQualityLanguageRepairBatchResult(
    IReadOnlyList<MetadataQualityLanguageRepairItemResult> Items);

public sealed record MetadataQualityLanguageRepairItemResult(
    Guid BookId,
    MetadataQualityLanguageRepairStatus Status,
    Book? Book = null,
    string? Message = null,
    IReadOnlyList<BookFileWriteBackResult>? FileResults = null);

public enum MetadataQualityLanguageRepairStatus
{
    Succeeded,
    SavedWithWriteBackErrors,
    NotFound,
    NotApplicable,
    Failed
}

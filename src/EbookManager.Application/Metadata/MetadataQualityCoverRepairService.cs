using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;

namespace EbookManager.Application.Metadata;

public interface IMetadataQualityCoverRepairService
{
    Task<MetadataQualityCoverRepairResult> RepairAsync(
        Guid bookId,
        byte[] coverBytes,
        CancellationToken cancellationToken);
}

public sealed class MetadataQualityCoverRepairService(
    IBookRepository bookRepository,
    BookService bookService,
    IBookCoverStore coverStore) : IMetadataQualityCoverRepairService
{
    private const int MaximumCoverBytes = 10 * 1024 * 1024;
    private readonly IBookRepository bookRepository = bookRepository;
    private readonly BookService bookService = bookService;
    private readonly IBookCoverStore coverStore = coverStore;

    public async Task<MetadataQualityCoverRepairResult> RepairAsync(
        Guid bookId,
        byte[] coverBytes,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(bookId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(coverBytes);
        if (coverBytes.Length is 0 or > MaximumCoverBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(coverBytes));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var currentBook = await bookRepository.GetAsync(bookId, cancellationToken);
        if (currentBook is null)
        {
            return new(bookId, MetadataQualityCoverRepairStatus.NotFound);
        }

        if (!MetadataQualitySignalEvaluator.Applies(currentBook, MetadataQualitySignalKeys.MissingCover))
        {
            return new(bookId, MetadataQualityCoverRepairStatus.NotApplicable, currentBook);
        }

        string relativePath;
        try
        {
            relativePath = await coverStore.SaveAsync(bookId, coverBytes, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new(bookId, MetadataQualityCoverRepairStatus.Failed, currentBook, exception.Message);
        }

        var updatedBook = currentBook with
        {
            Metadata = CopyMetadataWithCover(currentBook.Metadata, coverBytes),
            CoverRelativePath = relativePath,
            UpdatedUtc = DateTimeOffset.UtcNow
        };
        BookSaveResult saveResult;
        try
        {
            saveResult = await bookService.SaveAsync(updatedBook, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await CleanupCancelledSaveAsync(bookId, relativePath, coverBytes);
            throw;
        }

        var reloadedBook = await bookRepository.GetAsync(bookId, CancellationToken.None);
        var databaseWasUpdated = ContainsCover(reloadedBook, relativePath, coverBytes);

        string? cleanupMessage = null;
        if (!databaseWasUpdated)
        {
            try
            {
                await coverStore.DeleteAsync(bookId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                cleanupMessage = exception.Message;
            }
        }

        var hasFileWriteBackError = saveResult.FileResults.Any(file =>
            file.Result.Status == MetadataWriteBackStatus.Failed);
        var status = databaseWasUpdated
            ? saveResult.Status == BookSaveStatus.Succeeded && !hasFileWriteBackError
                ? MetadataQualityCoverRepairStatus.Succeeded
                : MetadataQualityCoverRepairStatus.SavedWithWriteBackErrors
            : MetadataQualityCoverRepairStatus.Failed;
        return new(
            bookId,
            status,
            reloadedBook,
            CombineMessages(saveResult.Message, cleanupMessage),
            saveResult.FileResults);
    }

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

    private async Task CleanupCancelledSaveAsync(
        Guid bookId,
        string relativePath,
        byte[] coverBytes)
    {
        try
        {
            var reloadedBook = await bookRepository.GetAsync(bookId, CancellationToken.None);
            if (!ContainsCover(reloadedBook, relativePath, coverBytes))
            {
                await coverStore.DeleteAsync(bookId, CancellationToken.None);
            }
        }
        catch (Exception)
        {
            // Preserve the original cancellation. If the persisted state cannot be verified,
            // retaining the file is safer than deleting a cover that may already be referenced.
        }
    }

    private static bool ContainsCover(Book? book, string relativePath, byte[] coverBytes) =>
        book is not null &&
        string.Equals(book.CoverRelativePath, relativePath, StringComparison.Ordinal) &&
        book.Metadata.CoverBytes?.SequenceEqual(coverBytes) == true;

    private static string? CombineMessages(string? first, string? second) =>
        string.Join(" ", new[] { first, second }.Where(value => !string.IsNullOrWhiteSpace(value))) is { Length: > 0 } message
            ? message
            : null;
}

public sealed record MetadataQualityCoverRepairResult(
    Guid BookId,
    MetadataQualityCoverRepairStatus Status,
    Book? Book = null,
    string? Message = null,
    IReadOnlyList<BookFileWriteBackResult>? FileResults = null);

public enum MetadataQualityCoverRepairStatus
{
    Succeeded,
    SavedWithWriteBackErrors,
    NotFound,
    NotApplicable,
    Failed
}

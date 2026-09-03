using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;

namespace EbookManager.Application.Metadata;

public interface IMetadataQualityTitleAuthorRepairService
{
    Task<MetadataQualityTitleAuthorRepairResult> RepairAsync(
        Guid bookId,
        CancellationToken cancellationToken);
}

public sealed class MetadataQualityTitleAuthorRepairService(
    IBookRepository bookRepository,
    BookService bookService) : IMetadataQualityTitleAuthorRepairService
{
    private readonly IBookRepository bookRepository = bookRepository;
    private readonly BookService bookService = bookService;

    public async Task<MetadataQualityTitleAuthorRepairResult> RepairAsync(
        Guid bookId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(bookId, Guid.Empty);
        cancellationToken.ThrowIfCancellationRequested();

        var currentBook = await bookRepository.GetAsync(bookId, cancellationToken);
        if (currentBook is null)
        {
            return new(bookId, MetadataQualityTitleAuthorRepairStatus.NotFound);
        }

        if (!CanRepair(currentBook))
        {
            return new(bookId, MetadataQualityTitleAuthorRepairStatus.NotApplicable, currentBook);
        }

        var currentTitle = currentBook.Metadata.Title.Trim();
        var currentAuthor = currentBook.Metadata.Authors[0].Trim();
        var updatedBook = currentBook with
        {
            Metadata = CopyMetadataWithSwappedTitleAndAuthor(
                currentBook.Metadata,
                currentAuthor,
                currentTitle),
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        var saveResult = await bookService.SaveAsync(updatedBook, cancellationToken);
        var reloadedBook = await bookRepository.GetAsync(bookId, cancellationToken);
        var databaseWasUpdated = reloadedBook?.Metadata.Title == currentAuthor &&
            reloadedBook.Metadata.Authors is [var storedAuthor] &&
            storedAuthor.Equals(currentTitle, StringComparison.Ordinal);
        var hasFileWriteBackError = saveResult.FileResults.Any(file =>
            file.Result.Status == MetadataWriteBackStatus.Failed);
        var status = saveResult.Status switch
        {
            BookSaveStatus.Succeeded when hasFileWriteBackError =>
                MetadataQualityTitleAuthorRepairStatus.SavedWithWriteBackErrors,
            BookSaveStatus.Succeeded => MetadataQualityTitleAuthorRepairStatus.Succeeded,
            BookSaveStatus.Failed when databaseWasUpdated =>
                MetadataQualityTitleAuthorRepairStatus.SavedWithWriteBackErrors,
            _ => MetadataQualityTitleAuthorRepairStatus.Failed
        };

        return new(
            bookId,
            status,
            reloadedBook,
            saveResult.Message,
            saveResult.FileResults);
    }

    private static bool CanRepair(Book book) =>
        MetadataQualitySignalEvaluator.Applies(
            book,
            MetadataQualitySignalKeys.PossibleTitleAuthorSwap) &&
        !string.IsNullOrWhiteSpace(book.Metadata.Title) &&
        book.Metadata.Authors is [var author] &&
        MetadataQualityAuthorRules.IsUsable(author);

    private static BookMetadata CopyMetadataWithSwappedTitleAndAuthor(
        BookMetadata metadata,
        string title,
        string author) =>
        new(
            title,
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

public sealed record MetadataQualityTitleAuthorRepairResult(
    Guid BookId,
    MetadataQualityTitleAuthorRepairStatus Status,
    Book? Book = null,
    string? Message = null,
    IReadOnlyList<BookFileWriteBackResult>? FileResults = null);

public enum MetadataQualityTitleAuthorRepairStatus
{
    Succeeded,
    SavedWithWriteBackErrors,
    NotFound,
    NotApplicable,
    Failed
}

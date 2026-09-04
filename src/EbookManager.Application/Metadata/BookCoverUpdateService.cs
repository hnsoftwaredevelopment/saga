using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;

namespace EbookManager.Application.Metadata;

public interface IBookCoverUpdateService
{
    Task<BookCoverUpdateResult> UpdateAsync(
        Book editedBook,
        byte[] coverBytes,
        CancellationToken cancellationToken);
}

public sealed class BookCoverUpdateService(
    IBookRepository bookRepository,
    BookService bookService,
    IBookCoverStore coverStore) : IBookCoverUpdateService
{
    private const int MaximumCoverBytes = 10 * 1024 * 1024;

    public async Task<BookCoverUpdateResult> UpdateAsync(
        Book editedBook,
        byte[] coverBytes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(editedBook);
        ArgumentNullException.ThrowIfNull(coverBytes);
        if (coverBytes.Length is 0 or > MaximumCoverBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(coverBytes));
        }

        var previous = await bookRepository.GetAsync(editedBook.Id, cancellationToken);
        if (previous is null)
        {
            return new(new BookSaveResult(BookSaveStatus.Failed, [], "The book no longer exists."));
        }

        string relativePath;
        try
        {
            relativePath = await coverStore.SaveAsync(editedBook.Id, coverBytes, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new(new BookSaveResult(BookSaveStatus.Failed, [], exception.Message), previous);
        }

        var updated = editedBook with
        {
            Metadata = CopyMetadataWithCover(editedBook.Metadata, coverBytes),
            CoverRelativePath = relativePath,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        BookSaveResult saveResult;
        try
        {
            saveResult = await bookService.SaveAsync(updated, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await RestorePreviousCoverAsync(previous);
            throw;
        }

        var reloaded = await bookRepository.GetAsync(editedBook.Id, CancellationToken.None);
        if (!ContainsCover(reloaded, relativePath, coverBytes))
        {
            await RestorePreviousCoverAsync(previous);
            if (saveResult.Status == BookSaveStatus.Succeeded)
            {
                saveResult = new BookSaveResult(
                    BookSaveStatus.Failed,
                    saveResult.FileResults,
                    "The cover could not be verified after saving.");
            }
        }

        return new(saveResult, reloaded);
    }

    private async Task RestorePreviousCoverAsync(Book previous)
    {
        try
        {
            if (previous.Metadata.CoverBytes is { Length: > 0 } previousBytes)
            {
                await coverStore.SaveAsync(previous.Id, previousBytes, CancellationToken.None);
            }
            else
            {
                await coverStore.DeleteAsync(previous.Id, CancellationToken.None);
            }
        }
        catch
        {
            // Preserve the original save outcome. Retrying remains possible from the editor.
        }
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

    private static bool ContainsCover(Book? book, string relativePath, byte[] coverBytes) =>
        book is not null &&
        string.Equals(book.CoverRelativePath, relativePath, StringComparison.Ordinal) &&
        book.Metadata.CoverBytes?.SequenceEqual(coverBytes) == true;
}

public sealed record BookCoverUpdateResult(BookSaveResult SaveResult, Book? Book = null);

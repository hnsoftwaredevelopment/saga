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

        using var operation = await BookCoverOperationLock.AcquireAsync(editedBook.Id, cancellationToken);

        Book? previous;
        try
        {
            previous = await bookRepository.GetAsync(editedBook.Id, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new(new BookSaveResult(BookSaveStatus.Failed, [], exception.Message));
        }

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
            var restoreFailure = await RestorePreviousCoverAsync(previous);
            if (restoreFailure is not null)
            {
                throw new InvalidOperationException(
                    "Saving was cancelled and the previous cover could not be restored. " + restoreFailure);
            }

            throw;
        }
        catch (Exception exception)
        {
            var restoreFailure = await RestorePreviousCoverAsync(previous);
            return new(
                new BookSaveResult(
                    BookSaveStatus.Failed,
                    [],
                    CombineMessages(exception.Message, RestoreFailureMessage(restoreFailure))),
                previous);
        }

        Book? reloaded;
        try
        {
            reloaded = await bookRepository.GetAsync(editedBook.Id, CancellationToken.None);
        }
        catch (Exception exception)
        {
            return new(
                new BookSaveResult(
                    BookSaveStatus.Failed,
                    saveResult.FileResults,
                    CombineMessages(
                        exception.Message,
                        "The new cover was retained because the saved database state could not be verified.")));
        }

        if (!ContainsCover(reloaded, relativePath, coverBytes))
        {
            var restoreFailure = await RestorePreviousCoverAsync(previous);
            saveResult = new BookSaveResult(
                saveResult.Status == BookSaveStatus.Succeeded ? BookSaveStatus.Failed : saveResult.Status,
                saveResult.FileResults,
                CombineMessages(
                    saveResult.Message ?? "The cover could not be verified after saving.",
                    RestoreFailureMessage(restoreFailure)));
        }

        return new(saveResult, reloaded);
    }

    private async Task<string?> RestorePreviousCoverAsync(Book previous)
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

            return null;
        }
        catch (Exception exception)
        {
            return exception.Message;
        }
    }

    private static string? RestoreFailureMessage(string? message) =>
        string.IsNullOrWhiteSpace(message) ? null : "The previous cover could not be restored: " + message;

    private static string? CombineMessages(string? first, string? second) =>
        string.Join(" ", new[] { first, second }.Where(value => !string.IsNullOrWhiteSpace(value))) is { Length: > 0 } message
            ? message
            : null;

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

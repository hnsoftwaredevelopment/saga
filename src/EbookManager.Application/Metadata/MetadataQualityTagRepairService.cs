using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;

namespace EbookManager.Application.Metadata;

public static class MetadataQualityTagNormalizer
{
    private static readonly char[] ValueSeparators = [',', '\r', '\n'];

    public static IReadOnlyList<string> Normalize(IEnumerable<string?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            foreach (var part in value.Split(
                         ValueSeparators,
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var tag = string.Join(
                    ' ',
                    part.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                if (tag.Length > 0 && seen.Add(tag))
                {
                    normalized.Add(tag);
                }
            }
        }

        return normalized;
    }
}

public interface IMetadataQualityTagRepairService
{
    Task<MetadataQualityTagRepairResult> RepairAsync(
        Guid bookId,
        IReadOnlyList<string> tags,
        CancellationToken cancellationToken);
}

public sealed class MetadataQualityTagRepairService(
    IBookRepository bookRepository,
    BookService bookService) : IMetadataQualityTagRepairService
{
    private readonly IBookRepository bookRepository = bookRepository;
    private readonly BookService bookService = bookService;

    public async Task<MetadataQualityTagRepairResult> RepairAsync(
        Guid bookId,
        IReadOnlyList<string> tags,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(bookId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(tags);

        var currentBook = await bookRepository.GetAsync(bookId, cancellationToken);
        if (currentBook is null)
        {
            return new(bookId, MetadataQualityTagRepairStatus.NotFound);
        }

        if (!MetadataQualitySignalEvaluator.Applies(currentBook, MetadataQualitySignalKeys.MessyTags))
        {
            return new(bookId, MetadataQualityTagRepairStatus.NotApplicable, currentBook);
        }

        var normalizedTags = MetadataQualityTagNormalizer.Normalize(tags);
        var updatedBook = currentBook with
        {
            Metadata = CopyMetadataWithTags(
                currentBook.Metadata,
                normalizedTags.Count == 0 ? null : normalizedTags),
            UpdatedUtc = DateTimeOffset.UtcNow
        };
        var saveResult = await bookService.SaveAsync(updatedBook, cancellationToken);
        var reloadedBook = await bookRepository.GetAsync(bookId, cancellationToken);
        var databaseWasUpdated = reloadedBook is not null &&
            NullableSequenceEqual(reloadedBook.Metadata.Tags, normalizedTags);
        var hasFileWriteBackError = saveResult.FileResults.Any(file =>
            file.Result.Status == MetadataWriteBackStatus.Failed);
        var status = saveResult.Status switch
        {
            BookSaveStatus.Succeeded when hasFileWriteBackError =>
                MetadataQualityTagRepairStatus.SavedWithWriteBackErrors,
            BookSaveStatus.Succeeded => MetadataQualityTagRepairStatus.Succeeded,
            BookSaveStatus.Failed when databaseWasUpdated =>
                MetadataQualityTagRepairStatus.SavedWithWriteBackErrors,
            _ => MetadataQualityTagRepairStatus.Failed
        };
        return new(
            bookId,
            status,
            reloadedBook,
            saveResult.Message,
            saveResult.FileResults);
    }

    private static bool NullableSequenceEqual(
        IReadOnlyList<string>? actual,
        IReadOnlyList<string> expected) =>
        (actual ?? []).SequenceEqual(expected, StringComparer.Ordinal);

    private static BookMetadata CopyMetadataWithTags(
        BookMetadata metadata,
        IReadOnlyList<string>? tags) =>
        new(
            metadata.Title,
            metadata.Authors,
            metadata.Description,
            metadata.Language,
            metadata.Publisher,
            metadata.PublicationDate,
            tags,
            metadata.Series,
            metadata.SeriesNumber,
            metadata.Isbn,
            metadata.CoverBytes);
}

public sealed record MetadataQualityTagRepairResult(
    Guid BookId,
    MetadataQualityTagRepairStatus Status,
    Book? Book = null,
    string? Message = null,
    IReadOnlyList<BookFileWriteBackResult>? FileResults = null);

public enum MetadataQualityTagRepairStatus
{
    Succeeded,
    SavedWithWriteBackErrors,
    NotFound,
    NotApplicable,
    Failed
}

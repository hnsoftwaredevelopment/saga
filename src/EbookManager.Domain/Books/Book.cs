namespace EbookManager.Domain.Books;

public sealed record Book(
    Guid Id,
    BookMetadata Metadata,
    ReadingStatus ReadingStatus,
    string? CoverRelativePath,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);

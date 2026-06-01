namespace EbookManager.Domain.Books;

public sealed record BookMetadata(
    string Title,
    IReadOnlyList<string> Authors,
    string? Description = null,
    string? Language = null,
    string? Publisher = null,
    DateOnly? PublicationDate = null,
    IReadOnlyList<string>? Tags = null,
    string? Series = null,
    decimal? SeriesNumber = null,
    string? Isbn = null,
    byte[]? CoverBytes = null);

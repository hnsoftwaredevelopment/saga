namespace EbookManager.Domain.Books;

public sealed record BookFile(
    Guid Id,
    Guid BookId,
    EbookFormat Format,
    string RelativePath,
    string Sha256,
    long SizeBytes,
    MetadataWriteBackStatus WriteBackStatus,
    string? WriteBackMessage);

namespace EbookManager.Domain.Books;

public sealed record BookDuplicateSnapshot(
    IReadOnlySet<string> FileHashes,
    IReadOnlySet<string> DuplicateKeys);

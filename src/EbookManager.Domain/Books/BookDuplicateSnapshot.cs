namespace EbookManager.Domain.Books;

public sealed record BookDuplicateSnapshot(
    IReadOnlySet<string> FileHashes,
    IReadOnlySet<string> DuplicateKeys)
{
    public IReadOnlyDictionary<string, Guid> BookIdsByFileHash { get; init; } =
        new Dictionary<string, Guid>(StringComparer.Ordinal);
}

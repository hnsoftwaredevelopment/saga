namespace EbookManager.Domain.Metadata;

public readonly record struct MetadataQualityExclusionKey
{
    public MetadataQualityExclusionKey(Guid bookId, string signalKey)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(bookId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(signalKey);

        BookId = bookId;
        SignalKey = signalKey.Trim();
    }

    public Guid BookId { get; }
    public string SignalKey { get; }
}

public sealed record MetadataQualityExclusion(
    MetadataQualityExclusionKey Key,
    string BookTitle,
    IReadOnlyList<string> BookAuthors,
    DateTimeOffset CreatedAt);

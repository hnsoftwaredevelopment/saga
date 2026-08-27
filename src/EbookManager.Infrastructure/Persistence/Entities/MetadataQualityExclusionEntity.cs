namespace EbookManager.Infrastructure.Persistence.Entities;

public sealed class MetadataQualityExclusionEntity
{
    public Guid BookId { get; set; }
    public string SignalKey { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public BookEntity Book { get; set; } = null!;
}

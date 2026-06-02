namespace EbookManager.Infrastructure.Persistence.Entities;

public sealed class ImportRunEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset StartedUtc { get; set; }
    public DateTimeOffset? CompletedUtc { get; set; }
    public ICollection<ImportItemEntity> Items { get; set; } = [];
}

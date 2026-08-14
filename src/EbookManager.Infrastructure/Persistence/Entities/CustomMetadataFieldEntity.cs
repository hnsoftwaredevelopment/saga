using EbookManager.Domain.CustomMetadata;

namespace EbookManager.Infrastructure.Persistence.Entities;

public sealed class CustomMetadataFieldEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public CustomMetadataFieldType Type { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
    public ICollection<CustomMetadataValueEntity> Values { get; set; } = [];
}

namespace EbookManager.Domain.CustomMetadata;

public sealed record CustomMetadataFieldDefinition(
    Guid Id,
    string Key,
    string Name,
    CustomMetadataFieldType Type,
    int SortOrder,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);

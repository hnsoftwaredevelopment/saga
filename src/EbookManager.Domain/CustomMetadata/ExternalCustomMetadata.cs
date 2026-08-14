namespace EbookManager.Domain.CustomMetadata;

public sealed record ExternalCustomMetadataField(
    string Key,
    string Name,
    CustomMetadataFieldType Type,
    IReadOnlyList<string> Options);

public sealed record ExternalCustomMetadataValue(
    ExternalCustomMetadataField Field,
    string? TextValue = null,
    decimal? NumberValue = null,
    DateOnly? DateValue = null,
    bool? BooleanValue = null);

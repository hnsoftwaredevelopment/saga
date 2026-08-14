namespace EbookManager.Domain.CustomMetadata;

public sealed record CustomMetadataValue(
    Guid BookId,
    Guid FieldId,
    string? TextValue = null,
    decimal? NumberValue = null,
    DateOnly? DateValue = null,
    bool? BooleanValue = null,
    DateTimeOffset? UpdatedUtc = null);

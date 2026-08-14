namespace EbookManager.Infrastructure.Persistence.Entities;

public sealed class CustomMetadataValueEntity
{
    public Guid BookId { get; set; }
    public BookEntity Book { get; set; } = null!;
    public Guid FieldId { get; set; }
    public CustomMetadataFieldEntity Field { get; set; } = null!;
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public DateOnly? DateValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}

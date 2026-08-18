namespace EbookManager.Infrastructure.Persistence.Entities;

public sealed class DuplicateExclusionEntity
{
    public Guid FirstBookId { get; set; }
    public Guid SecondBookId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public BookEntity FirstBook { get; set; } = null!;
    public BookEntity SecondBook { get; set; } = null!;
}

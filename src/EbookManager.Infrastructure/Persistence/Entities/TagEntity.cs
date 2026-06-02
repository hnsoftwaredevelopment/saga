namespace EbookManager.Infrastructure.Persistence.Entities;

public sealed class TagEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public ICollection<BookTagEntity> BookTags { get; set; } = [];
}

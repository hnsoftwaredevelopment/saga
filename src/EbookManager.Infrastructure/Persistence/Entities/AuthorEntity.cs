namespace EbookManager.Infrastructure.Persistence.Entities;

public sealed class AuthorEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Biography { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? PhotoRelativePath { get; set; }
    public ICollection<BookAuthorEntity> BookAuthors { get; set; } = [];
}

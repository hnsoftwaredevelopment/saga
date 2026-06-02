using EbookManager.Domain.Books;

namespace EbookManager.Infrastructure.Persistence.Entities;

public sealed class BookEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string NormalizedTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Language { get; set; }
    public string? Publisher { get; set; }
    public DateOnly? PublicationDate { get; set; }
    public string? Series { get; set; }
    public decimal? SeriesNumber { get; set; }
    public string? Isbn { get; set; }
    public byte[]? CoverBytes { get; set; }
    public ReadingStatus ReadingStatus { get; set; }
    public string? CoverRelativePath { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
    public ICollection<BookAuthorEntity> BookAuthors { get; set; } = [];
    public ICollection<BookTagEntity> BookTags { get; set; } = [];
    public ICollection<BookFileEntity> Files { get; set; } = [];
}

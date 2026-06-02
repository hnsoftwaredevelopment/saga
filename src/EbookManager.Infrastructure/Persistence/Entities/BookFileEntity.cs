using EbookManager.Domain.Books;

namespace EbookManager.Infrastructure.Persistence.Entities;

public sealed class BookFileEntity
{
    public Guid Id { get; set; }
    public Guid BookId { get; set; }
    public BookEntity Book { get; set; } = null!;
    public EbookFormat Format { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public MetadataWriteBackStatus WriteBackStatus { get; set; }
    public string? WriteBackMessage { get; set; }
}

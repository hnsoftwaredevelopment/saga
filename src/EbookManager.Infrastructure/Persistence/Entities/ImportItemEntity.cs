using EbookManager.Domain.Importing;
using EbookManager.Domain.Books;

namespace EbookManager.Infrastructure.Persistence.Entities;

public sealed class ImportItemEntity
{
    public Guid Id { get; set; }
    public Guid ImportRunId { get; set; }
    public ImportRunEntity ImportRun { get; set; } = null!;
    public int Sequence { get; set; }
    public string SourcePath { get; set; } = string.Empty;
    public ImportOutcome Outcome { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? BookId { get; set; }
    public BookEntity? Book { get; set; }
    public long? DurationMilliseconds { get; set; }
    public long? SizeBytes { get; set; }
    public EbookFormat? Format { get; set; }
    public string? SuggestionKind { get; set; }
    public Guid? SuggestedBookId { get; set; }
    public string? SuggestedTitle { get; set; }
    public string? SuggestedAuthors { get; set; }
}

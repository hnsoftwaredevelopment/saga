using EbookManager.Domain.Importing;

namespace EbookManager.Infrastructure.Persistence.Entities;

public sealed class ImportItemEntity
{
    public Guid Id { get; set; }
    public Guid ImportRunId { get; set; }
    public ImportRunEntity ImportRun { get; set; } = null!;
    public string SourcePath { get; set; } = string.Empty;
    public ImportOutcome Outcome { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? BookId { get; set; }
    public BookEntity? Book { get; set; }
}

using EbookManager.Domain.Books;

namespace EbookManager.Domain.Importing;

public enum ImportOutcome
{
    Added,
    ExactDuplicate,
    PossibleDuplicate,
    Failed
}

public sealed record ImportItemResult(
    string SourcePath,
    ImportOutcome Outcome,
    string Message,
    Guid? BookId = null);

public sealed record ImportBatchResult(IReadOnlyList<ImportItemResult> Items);

public sealed record MetadataReadResult(BookMetadata Metadata, string? Warning = null);

public sealed record MetadataWriteResult(MetadataWriteBackStatus Status, string? Message = null);

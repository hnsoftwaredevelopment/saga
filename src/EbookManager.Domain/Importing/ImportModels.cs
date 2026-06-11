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

public sealed record ImportBatchResult(
    Guid RunId,
    IReadOnlyList<ImportItemResult> Items,
    bool WasCancelled = false);

public sealed record ImportRunResult(
    Guid Id,
    DateTimeOffset StartedUtc,
    DateTimeOffset? CompletedUtc,
    IReadOnlyList<ImportItemResult> Items);

public sealed record ImportRunSummary(
    Guid Id,
    DateTimeOffset StartedUtc,
    DateTimeOffset? CompletedUtc,
    int TotalCount,
    int AddedCount,
    int ExactDuplicateCount,
    int PossibleDuplicateCount,
    int FailedCount)
{
    public int SkippedCount => ExactDuplicateCount + PossibleDuplicateCount;
}

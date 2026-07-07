using EbookManager.Domain.Books;

namespace EbookManager.Domain.Importing;

public enum ImportOutcome
{
    Added,
    ExactDuplicate,
    PossibleDuplicate,
    Failed
}

public enum ImportRunKind
{
    FileImport,
    DirectoryScan
}

public enum ImportItemSuggestionKind
{
    TitleMatch
}

public sealed record ImportRunContext(
    ImportRunKind Kind,
    string? SourcePath = null,
    bool? IncludeSubdirectories = null)
{
    public static ImportRunContext FileImport { get; } = new(ImportRunKind.FileImport);
}

public sealed record ImportItemResult(
    string SourcePath,
    ImportOutcome Outcome,
    string Message,
    Guid? BookId = null,
    ImportItemDiagnostics? Diagnostics = null,
    ImportItemSuggestion? Suggestion = null);

public sealed record ImportItemDiagnostics(
    TimeSpan Duration,
    long? SizeBytes = null,
    EbookFormat? Format = null);

public sealed record ImportItemSuggestion(
    ImportItemSuggestionKind Kind,
    Guid TargetBookId,
    string Title,
    string Authors);

public sealed record ImportBatchResult(
    Guid RunId,
    IReadOnlyList<ImportItemResult> Items,
    bool WasCancelled = false);

public sealed record ImportRunResult(
    Guid Id,
    DateTimeOffset StartedUtc,
    DateTimeOffset? CompletedUtc,
    IReadOnlyList<ImportItemResult> Items,
    ImportRunContext? Context = null);

public sealed record ImportRunSummary(
    Guid Id,
    DateTimeOffset StartedUtc,
    DateTimeOffset? CompletedUtc,
    int TotalCount,
    int AddedCount,
    int ExactDuplicateCount,
    int PossibleDuplicateCount,
    int FailedCount,
    ImportRunContext? Context = null)
{
    public int SkippedCount => ExactDuplicateCount + PossibleDuplicateCount;
}

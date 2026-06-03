using EbookManager.Domain.Importing;

namespace EbookManager.Presentation.ViewModels;

public sealed class ImportResultViewModel
{
    public ImportResultViewModel(ImportBatchResult result)
    {
        RunId = result.RunId;
        Items = result.Items
            .Select(item => new ImportResultItemViewModel(item))
            .ToList()
            .AsReadOnly();
    }

    public Guid RunId { get; }
    public IReadOnlyList<ImportResultItemViewModel> Items { get; }
    public int TotalCount => Items.Count;
    public int AddedCount => Count(ImportOutcome.Added);
    public int ExactDuplicateCount => Count(ImportOutcome.ExactDuplicate);
    public int PossibleDuplicateCount => Count(ImportOutcome.PossibleDuplicate);
    public int SkippedCount => ExactDuplicateCount + PossibleDuplicateCount;
    public int FailedCount => Count(ImportOutcome.Failed);
    public bool HasProblems => SkippedCount > 0 || FailedCount > 0;
    public string SummaryText =>
        $"{TotalCount} files processed: {AddedCount} added, {SkippedCount} skipped, {FailedCount} failed.";

    private int Count(ImportOutcome outcome) => Items.Count(item => item.Outcome == outcome);
}

public sealed class ImportResultItemViewModel(ImportItemResult item)
{
    public string SourcePath { get; } = item.SourcePath;
    public string FileName { get; } = Path.GetFileName(item.SourcePath);
    public ImportOutcome Outcome { get; } = item.Outcome;
    public string OutcomeLabel { get; } = item.Outcome switch
    {
        ImportOutcome.Added => "Added",
        ImportOutcome.ExactDuplicate => "Skipped duplicate",
        ImportOutcome.PossibleDuplicate => "Possible duplicate",
        ImportOutcome.Failed => "Failed",
        _ => item.Outcome.ToString()
    };
    public string Message { get; } = item.Message;
    public Guid? BookId { get; } = item.BookId;
}

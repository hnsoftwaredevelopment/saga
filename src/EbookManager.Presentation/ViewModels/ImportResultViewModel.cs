using System.Collections.ObjectModel;
using EbookManager.Domain.Importing;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EbookManager.Presentation.ViewModels;

public enum ImportResultOutcomeFilter
{
    All,
    Added,
    ExactDuplicate,
    PossibleDuplicate,
    Failed
}

public sealed partial class ImportResultViewModel : ObservableObject
{
    public ImportResultViewModel(ImportRunResult result)
        : this(new ImportBatchResult(result.Id, result.Items))
    {
    }

    public ImportResultViewModel(ImportBatchResult result)
    {
        RunId = result.RunId;
        Items = result.Items
            .Select(item => new ImportResultItemViewModel(item))
            .ToList()
            .AsReadOnly();
        OutcomeFilterOptions = Enum.GetValues<ImportResultOutcomeFilter>();
        RefreshVisibleItems();
    }

    public Guid RunId { get; }
    public IReadOnlyList<ImportResultItemViewModel> Items { get; }
    public IReadOnlyList<ImportResultOutcomeFilter> OutcomeFilterOptions { get; }
    public ObservableCollection<ImportResultItemViewModel> VisibleItems { get; } = [];
    public int TotalCount => Items.Count;
    public int AddedCount => Count(ImportOutcome.Added);
    public int ExactDuplicateCount => Count(ImportOutcome.ExactDuplicate);
    public int PossibleDuplicateCount => Count(ImportOutcome.PossibleDuplicate);
    public int SkippedCount => ExactDuplicateCount + PossibleDuplicateCount;
    public int FailedCount => Count(ImportOutcome.Failed);
    public bool HasProblems => SkippedCount > 0 || FailedCount > 0;
    public string SummaryText =>
        $"{TotalCount} files processed: {AddedCount} added, {SkippedCount} skipped, {FailedCount} failed.";

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private ImportResultOutcomeFilter selectedOutcomeFilter = ImportResultOutcomeFilter.All;

    private int Count(ImportOutcome outcome) => Items.Count(item => item.Outcome == outcome);

    partial void OnSearchTextChanged(string value) => RefreshVisibleItems();

    partial void OnSelectedOutcomeFilterChanged(ImportResultOutcomeFilter value) => RefreshVisibleItems();

    private void RefreshVisibleItems()
    {
        var query = Items.AsEnumerable();
        if (SelectedOutcomeFilter != ImportResultOutcomeFilter.All)
        {
            query = query.Where(item => SelectedOutcomeFilter switch
            {
                ImportResultOutcomeFilter.Added => item.Outcome == ImportOutcome.Added,
                ImportResultOutcomeFilter.ExactDuplicate => item.Outcome == ImportOutcome.ExactDuplicate,
                ImportResultOutcomeFilter.PossibleDuplicate => item.Outcome == ImportOutcome.PossibleDuplicate,
                ImportResultOutcomeFilter.Failed => item.Outcome == ImportOutcome.Failed,
                _ => true
            });
        }

        var search = SearchText.Trim();
        if (search.Length > 0)
        {
            query = query.Where(item => item.Matches(search));
        }

        VisibleItems.Clear();
        foreach (var item in query)
        {
            VisibleItems.Add(item);
        }
    }
}

public sealed class ImportHistoryViewModel(IEnumerable<ImportRunSummary> summaries)
{
    public IReadOnlyList<ImportRunSummaryViewModel> Items { get; } = summaries
        .Select(summary => new ImportRunSummaryViewModel(summary))
        .ToList()
        .AsReadOnly();

    public bool HasItems => Items.Count > 0;
}

public sealed class ImportRunSummaryViewModel(ImportRunSummary summary)
{
    public Guid RunId { get; } = summary.Id;
    public DateTimeOffset StartedUtc { get; } = summary.StartedUtc;
    public DateTimeOffset? CompletedUtc { get; } = summary.CompletedUtc;
    public ImportRunContext? Context { get; } = summary.Context;
    public int TotalCount { get; } = summary.TotalCount;
    public int AddedCount { get; } = summary.AddedCount;
    public int SkippedCount { get; } = summary.SkippedCount;
    public int FailedCount { get; } = summary.FailedCount;
    public string StartedText { get; } = summary.StartedUtc.ToLocalTime().ToString("g");
    public string CompletedText { get; } = summary.CompletedUtc?.ToLocalTime().ToString("g") ?? string.Empty;
    public string KindText { get; } = summary.Context?.Kind.ToString() ?? ImportRunKind.FileImport.ToString();
    public string SourceText { get; } = string.IsNullOrWhiteSpace(summary.Context?.SourcePath)
        ? string.Empty
        : summary.Context.SourcePath;
    public string DurationText { get; } = summary.CompletedUtc is null
        ? string.Empty
        : FormatDuration(summary.CompletedUtc.Value - summary.StartedUtc);

    private static string FormatDuration(TimeSpan duration) =>
        duration.TotalHours >= 1
            ? duration.ToString(@"h\:mm\:ss")
            : duration.ToString(@"m\:ss");
}

public sealed class ImportResultItemViewModel(ImportItemResult item)
{
    public string SourcePath { get; } = item.SourcePath;
    public string FileName { get; } = Path.GetFileName(item.SourcePath);
    public string FormatText { get; } = item.Diagnostics?.Format?.ToString().ToUpperInvariant() ?? string.Empty;
    public string SizeText { get; } = FormatSize(item.Diagnostics?.SizeBytes);
    public string DurationText { get; } = FormatDuration(item.Diagnostics?.Duration);
    public long SizeBytesSort { get; } = item.Diagnostics?.SizeBytes ?? -1;
    public double DurationMillisecondsSort { get; } = item.Diagnostics?.Duration.TotalMilliseconds ?? -1;
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

    public bool Matches(string searchText)
    {
        return Contains(FileName, searchText) ||
            Contains(SourcePath, searchText) ||
            Contains(FormatText, searchText) ||
            Contains(SizeText, searchText) ||
            Contains(DurationText, searchText) ||
            Contains(OutcomeLabel, searchText) ||
            Contains(Message, searchText);
    }

    private static bool Contains(string value, string searchText) =>
        value.Contains(searchText, StringComparison.CurrentCultureIgnoreCase);

    private static string FormatSize(long? bytes)
    {
        if (bytes is null)
        {
            return string.Empty;
        }

        var value = (double)bytes.Value;
        var units = new[] { "B", "KB", "MB", "GB" };
        var unitIndex = 0;
        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{bytes.Value.ToString(CultureInfo.CurrentCulture)} {units[unitIndex]}"
            : $"{value.ToString("0.#", CultureInfo.CurrentCulture)} {units[unitIndex]}";
    }

    private static string FormatDuration(TimeSpan? duration)
    {
        if (duration is null)
        {
            return string.Empty;
        }

        return duration.Value.TotalSeconds < 1
            ? $"{Math.Max(1, (int)Math.Round(duration.Value.TotalMilliseconds)).ToString(CultureInfo.CurrentCulture)} ms"
            : duration.Value.TotalMinutes < 1
                ? $"{duration.Value.TotalSeconds.ToString("0.0", CultureInfo.CurrentCulture)} s"
                : duration.Value.TotalHours < 1
                    ? duration.Value.ToString(@"m\:ss", CultureInfo.CurrentCulture)
                    : duration.Value.ToString(@"h\:mm\:ss", CultureInfo.CurrentCulture);
    }
}

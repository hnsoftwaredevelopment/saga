using EbookManager.Domain.Importing;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
    private readonly Func<IReadOnlyList<string>, CancellationToken, Task>? retryFailedAsync;
    private readonly AsyncRelayCommand retryFailedCommand;

    public ImportResultViewModel(
        ImportRunResult result,
        Func<IReadOnlyList<string>, CancellationToken, Task>? retryFailedAsync = null,
        Func<Guid, Guid, CancellationToken, Task>? linkSuggestionAsync = null,
        Func<string, string>? phaseNameLocalizer = null)
        : this(new ImportBatchResult(result.Id, result.Items), retryFailedAsync, linkSuggestionAsync, phaseNameLocalizer)
    {
    }

    public ImportResultViewModel(
        ImportBatchResult result,
        Func<IReadOnlyList<string>, CancellationToken, Task>? retryFailedAsync = null,
        Func<Guid, Guid, CancellationToken, Task>? linkSuggestionAsync = null,
        Func<string, string>? phaseNameLocalizer = null)
    {
        this.retryFailedAsync = retryFailedAsync;
        var resolvedPhaseNameLocalizer = phaseNameLocalizer ?? DefaultPhaseName;
        RunId = result.RunId;
        Items = result.Items
            .Select(item => new ImportResultItemViewModel(item, linkSuggestionAsync, resolvedPhaseNameLocalizer))
            .ToList()
            .AsReadOnly();
        PhaseSummaries = CreatePhaseSummaries(result.Items, resolvedPhaseNameLocalizer);
        OutcomeFilterOptions = Enum.GetValues<ImportResultOutcomeFilter>();
        retryFailedCommand = new AsyncRelayCommand(RetryFailedImportsAsync, () => CanRetryFailedImports);
        RefreshVisibleItems();
    }

    public Guid RunId { get; }
    public IReadOnlyList<ImportResultItemViewModel> Items { get; }
    public IReadOnlyList<ImportPhaseSummaryViewModel> PhaseSummaries { get; }
    public IReadOnlyList<ImportResultOutcomeFilter> OutcomeFilterOptions { get; }
    public BulkObservableCollection<ImportResultItemViewModel> VisibleItems { get; } = [];
    public int TotalCount => Items.Count;
    public int AddedCount => Count(ImportOutcome.Added);
    public int ExactDuplicateCount => Count(ImportOutcome.ExactDuplicate);
    public int PossibleDuplicateCount => Count(ImportOutcome.PossibleDuplicate);
    public int SkippedCount => ExactDuplicateCount + PossibleDuplicateCount;
    public int FailedCount => Count(ImportOutcome.Failed);
    public bool HasProblems => SkippedCount > 0 || FailedCount > 0;
    public int RetryFailedCount => GetRetryFailedSourcePaths().Count;
    public bool CanRetryFailedImports => retryFailedAsync is not null && RetryFailedCount > 0;
    public IAsyncRelayCommand RetryFailedCommand => retryFailedCommand;
    public string SummaryText =>
        $"{TotalCount} files processed: {AddedCount} added, {SkippedCount} skipped, {FailedCount} failed.";
    public bool HasPhaseSummaries => PhaseSummaries.Count > 0;
    public string PhaseSummaryText => string.Join(
        "; ",
        PhaseSummaries.Select(summary => $"{summary.DisplayName} {summary.DurationText} ({summary.PercentageText})"));

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private ImportResultOutcomeFilter selectedOutcomeFilter = ImportResultOutcomeFilter.All;

    private int Count(ImportOutcome outcome) => Items.Count(item => item.Outcome == outcome);

    partial void OnSearchTextChanged(string value) => RefreshVisibleItems();

    partial void OnSelectedOutcomeFilterChanged(ImportResultOutcomeFilter value) => RefreshVisibleItems();

    private async Task RetryFailedImportsAsync(CancellationToken cancellationToken)
    {
        if (retryFailedAsync is null)
        {
            return;
        }

        var paths = GetRetryFailedSourcePaths();
        if (paths.Count == 0)
        {
            return;
        }

        await retryFailedAsync(paths, cancellationToken);
    }

    private IReadOnlyList<string> GetRetryFailedSourcePaths()
    {
        return Items
            .Where(item => item.Outcome == ImportOutcome.Failed && IsRetryableSourcePath(item.SourcePath))
            .Select(item => item.SourcePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    private static bool IsRetryableSourcePath(string sourcePath)
    {
        return !string.IsNullOrWhiteSpace(sourcePath) &&
            Path.IsPathFullyQualified(sourcePath) &&
            File.Exists(sourcePath);
    }

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

        VisibleItems.ReplaceAll(query.ToArray());
    }

    private static IReadOnlyList<ImportPhaseSummaryViewModel> CreatePhaseSummaries(
        IEnumerable<ImportItemResult> items,
        Func<string, string> phaseNameLocalizer)
    {
        var totals = new Dictionary<string, TimeSpan>(StringComparer.Ordinal)
        {
            ["local"] = TimeSpan.Zero,
            ["size"] = TimeSpan.Zero,
            ["hash"] = TimeSpan.Zero,
            ["meta"] = TimeSpan.Zero,
            ["dup"] = TimeSpan.Zero,
            ["copy"] = TimeSpan.Zero,
            ["db"] = TimeSpan.Zero,
            ["cleanup"] = TimeSpan.Zero
        };

        foreach (var timings in items.Select(item => item.Diagnostics?.PhaseTimings).OfType<ImportPhaseTimings>())
        {
            Add(totals, "local", timings.AvailabilityCheck);
            Add(totals, "size", timings.SizeRead);
            Add(totals, "hash", timings.Hashing);
            Add(totals, "meta", timings.MetadataRead);
            Add(totals, "dup", timings.DuplicateCheck);
            Add(totals, "copy", timings.ManagedCopy);
            Add(totals, "db", timings.DatabaseSave);
            Add(totals, "cleanup", timings.Cleanup);
        }

        var totalMilliseconds = totals.Values.Sum(duration => duration.TotalMilliseconds);
        if (totalMilliseconds <= 0)
        {
            return [];
        }

        return totals
            .Where(total => total.Value > TimeSpan.Zero)
            .OrderByDescending(total => total.Value)
            .Select(total => new ImportPhaseSummaryViewModel(
                total.Key,
                phaseNameLocalizer(total.Key),
                total.Value,
                total.Value.TotalMilliseconds / totalMilliseconds * 100))
            .ToList()
            .AsReadOnly();

        static void Add(IDictionary<string, TimeSpan> totals, string name, TimeSpan? duration)
        {
            if (duration is not null)
            {
                totals[name] += duration.Value;
            }
        }
    }

    internal static string DefaultPhaseName(string name) =>
        name switch
        {
            "local" => "File availability",
            "size" => "File size",
            "hash" => "File recognition",
            "meta" => "Metadata",
            "dup" => "Duplicate check",
            "copy" => "Copy to library",
            "db" => "Save data",
            "cleanup" => "Cleanup",
            _ => name
        };
}

public sealed class ImportPhaseSummaryViewModel(string name, string displayName, TimeSpan duration, double percentage)
{
    public string Name { get; } = name;
    public string DisplayName { get; } = displayName;
    public TimeSpan Duration { get; } = duration;
    public double Percentage { get; } = percentage;
    public string DurationText { get; } = ImportResultFormatting.FormatDuration(duration);
    public string PercentageText { get; } = percentage.ToString("0.#", CultureInfo.CurrentCulture) + "%";
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

public sealed class ImportResultItemViewModel : ObservableObject
{
    private readonly Guid? bookId;
    private readonly ImportItemSuggestion? suggestion;
    private readonly Func<Guid, Guid, CancellationToken, Task>? linkSuggestionAsync;
    private readonly AsyncRelayCommand linkSuggestionCommand;
    private bool suggestionLinked;

    public ImportResultItemViewModel(
        ImportItemResult item,
        Func<Guid, Guid, CancellationToken, Task>? linkSuggestionAsync = null,
        Func<string, string>? phaseNameLocalizer = null)
    {
        this.linkSuggestionAsync = linkSuggestionAsync;
        bookId = item.BookId;
        suggestion = item.Suggestion;
        SourcePath = item.SourcePath;
        FileName = Path.GetFileName(item.SourcePath);
        FormatText = item.Diagnostics?.Format?.ToString().ToUpperInvariant() ?? string.Empty;
        SizeText = FormatSize(item.Diagnostics?.SizeBytes);
        DurationText = ImportResultFormatting.FormatDuration(item.Diagnostics?.Duration);
        PhaseTimingsText = FormatPhaseTimings(item.Diagnostics?.PhaseTimings, phaseNameLocalizer ?? ImportResultViewModel.DefaultPhaseName);
        SizeBytesSort = item.Diagnostics?.SizeBytes ?? -1;
        DurationMillisecondsSort = item.Diagnostics?.Duration.TotalMilliseconds ?? -1;
        Outcome = item.Outcome;
        OutcomeLabel = item.Outcome switch
        {
            ImportOutcome.Added => "Added",
            ImportOutcome.ExactDuplicate => "Skipped duplicate",
            ImportOutcome.PossibleDuplicate => "Possible duplicate",
            ImportOutcome.Failed => "Failed",
            _ => item.Outcome.ToString()
        };
        Message = item.Message;
        BookId = item.BookId;
        SuggestionText = item.Suggestion is null
            ? string.Empty
            : string.IsNullOrWhiteSpace(item.Suggestion.Authors)
                ? item.Suggestion.Title
                : $"{item.Suggestion.Title} - {item.Suggestion.Authors}";
        linkSuggestionCommand = new AsyncRelayCommand(LinkSuggestionAsync, () => CanLinkSuggestion);
    }

    public string SourcePath { get; }
    public string FileName { get; }
    public string FormatText { get; }
    public string SizeText { get; }
    public string DurationText { get; }
    public string PhaseTimingsText { get; }
    public long SizeBytesSort { get; }
    public double DurationMillisecondsSort { get; }
    public ImportOutcome Outcome { get; }
    public string OutcomeLabel { get; }
    public string Message { get; }
    public Guid? BookId { get; }
    public string SuggestionText { get; }
    public string LinkSuggestionLabel => SuggestionLinked ? "Linked" : "Link";
    public IAsyncRelayCommand LinkSuggestionCommand => linkSuggestionCommand;

    public bool CanLinkSuggestion =>
        !suggestionLinked &&
        linkSuggestionAsync is not null &&
        Outcome == ImportOutcome.Added &&
        bookId is { } sourceBookId &&
        suggestion is { Kind: ImportItemSuggestionKind.TitleMatch } &&
        sourceBookId != suggestion.TargetBookId;

    public bool SuggestionLinked
    {
        get => suggestionLinked;
        private set
        {
            if (SetProperty(ref suggestionLinked, value))
            {
                OnPropertyChanged(nameof(CanLinkSuggestion));
                OnPropertyChanged(nameof(LinkSuggestionLabel));
                linkSuggestionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    private async Task LinkSuggestionAsync(CancellationToken cancellationToken)
    {
        if (!CanLinkSuggestion ||
            linkSuggestionAsync is null ||
            bookId is not { } sourceBookId ||
            suggestion is null)
        {
            return;
        }

        await linkSuggestionAsync(sourceBookId, suggestion.TargetBookId, cancellationToken);
        SuggestionLinked = true;
    }

    public bool Matches(string searchText)
    {
        return Contains(FileName, searchText) ||
            Contains(SourcePath, searchText) ||
            Contains(FormatText, searchText) ||
            Contains(SizeText, searchText) ||
            Contains(DurationText, searchText) ||
            Contains(PhaseTimingsText, searchText) ||
            Contains(OutcomeLabel, searchText) ||
            Contains(Message, searchText) ||
            Contains(SuggestionText, searchText);
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

    private static string FormatPhaseTimings(ImportPhaseTimings? timings, Func<string, string> phaseNameLocalizer)
    {
        if (timings is null)
        {
            return string.Empty;
        }

        var parts = new[]
            {
                ("local", timings.AvailabilityCheck),
                ("size", timings.SizeRead),
                ("hash", timings.Hashing),
                ("meta", timings.MetadataRead),
                ("dup", timings.DuplicateCheck),
                ("copy", timings.ManagedCopy),
                ("db", timings.DatabaseSave),
                ("cleanup", timings.Cleanup)
            }
            .Where(part => part.Item2 is not null)
            .Select(part => $"{phaseNameLocalizer(part.Item1)} {ImportResultFormatting.FormatDuration(part.Item2)}")
            .ToArray();

        return string.Join("; ", parts);
    }
}

internal static class ImportResultFormatting
{
    public static string FormatDuration(TimeSpan? duration)
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

namespace EbookManager.Presentation.ViewModels;

public sealed record LibraryPerformanceSnapshot(
    string Operation,
    TimeSpan TotalDuration,
    int BookCount,
    int VisibleBookCount,
    int GroupCount,
    IReadOnlyList<LibraryGroupOption> Groupings,
    LibrarySortOption SortOption,
    IReadOnlyDictionary<string, TimeSpan> Phases);

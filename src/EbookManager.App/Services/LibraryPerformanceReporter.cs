using System.IO;
using EbookManager.Presentation.Abstractions;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Services;

public sealed class LibraryPerformanceReporter : ILibraryPerformanceReporter
{
    private static readonly TimeSpan SlowOperationThreshold = TimeSpan.FromSeconds(2);
    private readonly string logPath;

    public LibraryPerformanceReporter()
    {
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Saga",
            "Logs");
        Directory.CreateDirectory(logDirectory);
        logPath = Path.Combine(logDirectory, "performance.log");
    }

    public void Report(LibraryPerformanceSnapshot snapshot)
    {
        if (snapshot.TotalDuration < SlowOperationThreshold && !ShouldAlwaysLog(snapshot))
        {
            return;
        }

        var groupings = snapshot.Groupings.Count == 0
            ? "-"
            : string.Join(">", snapshot.Groupings);
        var phases = snapshot.Phases.Count == 0
            ? "-"
            : string.Join(", ", snapshot.Phases.Select(phase => $"{phase.Key}={phase.Value.TotalMilliseconds:0}ms"));
        var line =
            $"{DateTimeOffset.Now:O}\t{snapshot.Operation}\ttotal={snapshot.TotalDuration.TotalMilliseconds:0}ms\tbooks={snapshot.BookCount}\tvisible={snapshot.VisibleBookCount}\tgroups={snapshot.GroupCount}\tsort={snapshot.SortOption}\tgroupings={groupings}\tphases={phases}";

        _ = Task.Run(async () =>
        {
            try
            {
                await File.AppendAllLinesAsync(logPath, [line]).ConfigureAwait(false);
            }
            catch
            {
            }
        });
    }

    private static bool ShouldAlwaysLog(LibraryPerformanceSnapshot snapshot) =>
        snapshot.Operation is "AddGrouping" or "RemoveGrouping" or "SetGroupingOptions";
}

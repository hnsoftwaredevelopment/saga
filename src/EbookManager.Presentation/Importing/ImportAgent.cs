using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Importing;
using EbookManager.Presentation.Abstractions;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.Presentation.Importing;

public sealed class ImportAgent(
    IImportRunner importRunner,
    ImportJobViewModel job) : IImportAgent
{
    private CancellationTokenSource? activeCancellation;

    public event EventHandler<ImportBatchResult>? Completed;

    public ImportJobViewModel Job { get; } = job;

    public Task? ActiveTask { get; private set; }

    public bool IsActive => ActiveTask is { IsCompleted: false };

    public Task StartImportAsync(
        IReadOnlyList<string> sourcePaths,
        Func<ImportProgress, Task> onProgress,
        CancellationToken cancellationToken)
    {
        if (IsActive)
        {
            throw new InvalidOperationException("An import job is already active.");
        }

        activeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Job.StartImport(Guid.Empty, sourcePaths.Count);
        ActiveTask = RunImportAsync(sourcePaths, onProgress, activeCancellation.Token);
        return Task.CompletedTask;
    }

    public void StartScanning() => Job.StartScanning();

    public void CancelActiveJob() => activeCancellation?.Cancel();

    private async Task RunImportAsync(
        IReadOnlyList<string> sourcePaths,
        Func<ImportProgress, Task> onProgress,
        CancellationToken cancellationToken)
    {
        try
        {
            var progress = new Progress<ImportProgress>(snapshot =>
            {
                Job.ApplyProgress(snapshot);
                _ = onProgress(snapshot);
            });
            var result = await importRunner.ImportAsync(sourcePaths, progress, cancellationToken);
            Job.Complete(result);
            Completed?.Invoke(this, result);
        }
        catch (OperationCanceledException)
        {
            Job.Cancelled();
        }
        catch
        {
            Job.Failed("The import job failed.");
        }
        finally
        {
            activeCancellation?.Dispose();
            activeCancellation = null;
        }
    }
}

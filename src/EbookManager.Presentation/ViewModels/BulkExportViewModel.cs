using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Application.Books;
using EbookManager.Domain.Books;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed record ExportFormatOption(EbookFormat Format, string Name);
public sealed record ExportRow(string Title, string Authors, string Outcome, string Destination = "");

public sealed class BulkExportViewModel(
    IReadOnlyList<BookExportSource> selection, IBulkBookExportService service, Func<string, string> localize) : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? operation;
    private IReadOnlyList<BookExportSource> preview = [];
    private bool disposed, initialized;
    private ExportFormatOption? selectedFormat;
    private string destination = "";
    public IReadOnlyList<ExportFormatOption> Formats { get; private set; } = [];
    public IReadOnlyList<ExportRow> Rows { get; private set; } = [];
    public ExportFormatOption? SelectedFormat
    {
        get => selectedFormat;
        set { if (CanConfigure && SetProperty(ref selectedFormat, value)) RefreshPreview(); }
    }
    public string Destination
    {
        get => destination;
        set { if (CanConfigure && SetProperty(ref destination, value)) NotifyState(); }
    }
    public bool IsLoading { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsCancelling { get; private set; }
    public bool HasFinished { get; private set; }
    public bool IsError { get; private set; }
    public bool IsSuccess { get; private set; }
    public bool IsIndeterminate { get; private set; }
    public int Completed { get; private set; }
    public int Total { get; private set; } = 1;
    public string Message { get; private set; } = "";
    public string Availability { get; private set; } = "";
    public string ProgressText { get; private set; } = "";
    public bool CanConfigure => !disposed && !IsLoading && !IsRunning && !HasFinished;
    public bool CanStart => CanConfigure && selectedFormat is not null && !string.IsNullOrWhiteSpace(destination)
        && preview.Any(book => book.Available && book.Files.Any(file => file.Format == selectedFormat.Format));
    public bool CanCancel => (IsLoading || IsRunning) && !IsCancelling && !disposed;
    public bool CanClose => !IsRunning;
    private string Text(string key, params object[] args) => string.Format(CultureInfo.CurrentCulture, localize(key), args);

    public async Task LoadAsync()
    {
        if (initialized || disposed) return;
        initialized = true; IsLoading = true; IsIndeterminate = true; Message = localize("BulkExportPreparing"); NotifyState();
        try
        {
            var token = lifetime.Token;
            var prepared = await Task.Run(() => service.PrepareAsync(selection, null, token));
            if (disposed) return;
            token.ThrowIfCancellationRequested();
            preview = prepared;
            Formats = preview.Where(book => book.Available).SelectMany(book => book.Files).Select(file => file.Format)
                .Where(format => Enum.IsDefined(format)).Distinct().OrderBy(format => format)
                .Select(format => new ExportFormatOption(format, format.ToString().ToUpperInvariant())).ToArray();
            selectedFormat = Formats.FirstOrDefault(item => item.Format == EbookFormat.Epub) ?? Formats.FirstOrDefault();
            OnPropertyChanged(nameof(Formats)); OnPropertyChanged(nameof(SelectedFormat));
            Message = ""; RefreshPreview();
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { if (!disposed) Message = localize("BulkExportCancelled"); }
        catch (Exception) { if (!disposed) { IsError = true; Message = localize("BulkExportLoadFailed"); } }
        finally { if (!disposed) { IsLoading = false; IsCancelling = false; IsIndeterminate = false; NotifyState(); } }
    }

    private void RefreshPreview()
    {
        var matching = preview.Where(book => book.Available).Select(book => book.Files.Count(file => file.Format == selectedFormat?.Format)).ToArray();
        Availability = Text("BulkExportAvailability", preview.Count, matching.Sum(), preview.Count - matching.Count(count => count > 0));
        Rows = preview.Select(book => new ExportRow(book.Title, string.Join(", ", book.Authors), !book.Available
            ? localize("BulkExportBookMissing") : book.Files.Any(file => file.Format == selectedFormat?.Format)
                ? Text("BulkExportFileCount", book.Files.Count(file => file.Format == selectedFormat?.Format)) : localize("BulkExportFormatMissing"))).ToArray();
        OnPropertyChanged(nameof(Rows)); OnPropertyChanged(nameof(Availability));
        NotifyState();
    }

    public async Task StartAsync()
    {
        if (!CanStart) return;
        var format = selectedFormat!.Format; var folder = destination;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        operation = cancellation; IsRunning = true; IsCancelling = false; IsIndeterminate = true;
        IsError = IsSuccess = false; Message = ""; Completed = 0; ProgressText = localize("BulkExportPreparing"); NotifyState();
        // The UI synchronization context receives reports; queued reports after completion are ignored.
        var uiProgress = new Progress<BulkExportProgress>(value =>
        {
            if (disposed || !IsRunning || IsCancelling) return;
            Completed = value.Completed; Total = Math.Max(1, value.Total); IsIndeterminate = value.Preparing;
            ProgressText = Text("BulkExportProgress", value.Completed, value.Total, value.Title); NotifyState();
        });
        var progress = new ExportProgress(uiProgress);
        try
        {
            var result = await Task.Run(() => service.ExportAsync(preview, format, folder, progress, cancellation.Token));
            if (disposed) return;
            Rows = result.Items.Select(item => new ExportRow(item.Title, item.Authors,
                localize("BulkExport" + item.Status), item.DestinationPath ?? "")).ToArray();
            OnPropertyChanged(nameof(Rows));
            var exported = result.Items.Count(item => item.Status == BulkExportStatus.Exported);
            var errors = result.Items.Count(item => item.Status is not (BulkExportStatus.Exported or BulkExportStatus.FormatMissing or BulkExportStatus.NotProcessed));
            var skipped = result.Items.Count(item => item.Status == BulkExportStatus.FormatMissing);
            var remaining = result.Items.Count(item => item.Status == BulkExportStatus.NotProcessed);
            IsError = errors > 0; IsSuccess = errors == 0 && !result.Cancelled;
            Message = errors == 0 ? Text("BulkExportSuccess", exported) + (skipped > 0 ? " " + Text("BulkExportSkipped", skipped) : "")
                : Text("BulkExportSummary", exported, skipped, errors);
            if (result.Cancelled) Message += " " + Text("BulkExportStopped", remaining);
            Completed = result.Items.Count - remaining; Total = Math.Max(1, result.Items.Count);
            ProgressText = "";
        }
        catch (Exception) { if (!disposed) { IsError = true; Message = localize("BulkExportUnexpected"); } }
        finally
        {
            operation = null;
            if (!disposed) { IsRunning = false; IsCancelling = false; IsIndeterminate = false; HasFinished = true; NotifyState(); }
        }
    }
    public void Cancel()
    {
        if (!CanCancel) return;
        IsCancelling = true; Message = localize("BulkExportCancelling");
        if (IsLoading) lifetime.Cancel(); else operation?.Cancel();
        NotifyState();
    }
    public void FolderFailed() { IsError = true; Message = localize("BulkExportFolderFailed"); NotifyState(); }
    private void NotifyState()
    {
        foreach (var property in new[] { nameof(IsLoading), nameof(IsRunning), nameof(IsCancelling), nameof(HasFinished),
            nameof(IsError), nameof(IsSuccess), nameof(IsIndeterminate), nameof(Completed), nameof(Total), nameof(Message),
            nameof(ProgressText), nameof(CanConfigure), nameof(CanStart), nameof(CanCancel), nameof(CanClose) })
            OnPropertyChanged(property);
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; lifetime.Cancel(); lifetime.Dispose();
    }

    private sealed class ExportProgress(IProgress<BulkExportProgress> target) : IProgress<BulkExportProgress>
    {
        private long last;
        public void Report(BulkExportProgress value)
        {
            var now = Environment.TickCount64;
            if (value.Preparing || value.Completed <= 1 || value.Completed == value.Total || now - last >= 100)
            { last = now; target.Report(value); }
        }
    }
}

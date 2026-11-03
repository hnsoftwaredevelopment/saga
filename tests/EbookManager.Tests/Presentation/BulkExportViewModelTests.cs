using EbookManager.Application.Books;
using EbookManager.Domain.Books;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;
using System.Collections.Concurrent;

namespace EbookManager.Tests.Presentation;

public sealed class BulkExportViewModelTests
{
    private static BookExportSource Source(EbookFormat format = EbookFormat.Epub) => new(Guid.NewGuid(), "Title", ["Author"],
        [new(Guid.NewGuid(), Guid.Empty, format, "file.epub", "hash", 10, MetadataWriteBackStatus.Succeeded, null)]);
    private static string Text(string key) => key switch
    {
        "BulkExportAvailability" => "{0} selected; {1} files; {2} skipped",
        "BulkExportFileCount" => "{0} files", "BulkExportSummary" => "{0} exported; {1} skipped; {2} failed",
        "BulkExportSuccess" => "{0} exported", "BulkExportStopped" => "{0} remaining",
        "BulkExportProgress" => "{0}/{1}: {2}", _ => key
    };
    [Fact]
    public async Task Preview_defaults_to_epub_counts_files_and_requires_a_destination()
    {
        var epub = Source(); var pdf = Source(EbookFormat.Pdf);
        using var vm = new BulkExportViewModel([epub, pdf], new Service([epub, pdf]), Text);
        await vm.LoadAsync();
        vm.SelectedFormat!.Format.Should().Be(EbookFormat.Epub);
        vm.Availability.Should().Be("2 selected; 1 files; 1 skipped");
        vm.Rows[1].Outcome.Should().Be("BulkExportFormatMissing"); vm.CanStart.Should().BeFalse();
        vm.Destination = "target"; vm.CanStart.Should().BeTrue();
        vm.SelectedFormat = vm.Formats.Single(item => item.Format == EbookFormat.Pdf);
        vm.Rows[0].Outcome.Should().Be("BulkExportFormatMissing");
        vm.Rows[1].Outcome.Should().Be("1 files");
    }
    [Fact]
    public async Task Successful_export_reports_short_success_keeps_destination_and_cannot_repeat()
    {
        var source = Source(); var service = new Service([source]);
        using var vm = new BulkExportViewModel([source], service, Text);
        await vm.LoadAsync(); vm.Destination = "target";
        await vm.StartAsync(); await vm.StartAsync();
        service.Starts.Should().Be(1); vm.IsSuccess.Should().BeTrue(); vm.IsError.Should().BeFalse();
        vm.Message.Should().Be("1 exported"); vm.Rows.Single().Destination.Should().Be("copied.epub");
        vm.Destination.Should().Be("target"); vm.HasFinished.Should().BeTrue(); vm.CanStart.Should().BeFalse(); vm.CanClose.Should().BeTrue();
    }
    [Fact]
    public async Task Cancelling_keeps_running_window_open_until_partial_results_are_available()
    {
        var source = Source(); var service = new Service([source]) { Hold = true };
        using var vm = new BulkExportViewModel([source], service, Text);
        await vm.LoadAsync(); vm.Destination = "target";
        var task = vm.StartAsync(); await service.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        vm.IsRunning.Should().BeTrue(); vm.CanConfigure.Should().BeFalse(); vm.CanClose.Should().BeFalse();
        vm.Cancel(); vm.CanCancel.Should().BeFalse(); vm.IsCancelling.Should().BeTrue();
        await task.WaitAsync(TimeSpan.FromSeconds(5));
        vm.Message.Should().Contain("1 remaining"); vm.IsError.Should().BeFalse(); vm.IsSuccess.Should().BeFalse();
        vm.Rows.Single().Outcome.Should().Be("BulkExportNotProcessed"); vm.CanClose.Should().BeTrue();
    }
    [Fact]
    public async Task Missing_formats_are_neutral_but_copy_failures_are_reported_as_errors()
    {
        var source = Source(); var service = new Service([source]) { Status = BulkExportStatus.Failed };
        using var vm = new BulkExportViewModel([source], service, Text);
        await vm.LoadAsync(); vm.Destination = "target"; await vm.StartAsync();
        vm.IsError.Should().BeTrue(); vm.IsSuccess.Should().BeFalse(); vm.Message.Should().Be("0 exported; 0 skipped; 1 failed");
        vm.Rows.Single().Outcome.Should().Be("BulkExportFailed");
    }
    [Fact]
    public async Task Read_failure_is_localized_and_empty_preview_cannot_start()
    {
        using var vm = new BulkExportViewModel([], new Service([]) { Fail = true }, Text);
        await vm.LoadAsync(); vm.Destination = "target";
        vm.IsError.Should().BeTrue(); vm.Message.Should().Be("BulkExportLoadFailed"); vm.CanStart.Should().BeFalse();
    }
    [Fact]
    public async Task Queued_progress_after_completion_cannot_replace_final_feedback_or_counts()
    {
        var source = Source(); var service = new Service([source]);
        using var vm = new BulkExportViewModel([source], service, Text);
        await vm.LoadAsync(); vm.Destination = "target";
        var queue = new QueuedContext(); var previous = SynchronizationContext.Current; Task export;
        try { SynchronizationContext.SetSynchronizationContext(queue); export = vm.StartAsync(); }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
        var end = DateTime.UtcNow.AddSeconds(5);
        while (!export.IsCompleted)
        {
            queue.Drain();
            if (DateTime.UtcNow > end) throw new TimeoutException();
            await Task.Delay(1);
        }
        await export;
        service.Progress!.Report(new(99, 100, "late")); queue.Drain();
        vm.Message.Should().Be("1 exported"); vm.Completed.Should().Be(1); vm.Total.Should().Be(1);
        vm.ProgressText.Should().BeEmpty(); vm.IsIndeterminate.Should().BeFalse();
    }
    [Fact]
    public async Task Closing_during_preparation_cancels_work_and_ignores_late_results()
    {
        var source = Source(); var service = new Service([source]) { HoldPreparation = true };
        var vm = new BulkExportViewModel([source], service, Text); var load = vm.LoadAsync();
        await service.Preparing.Task.WaitAsync(TimeSpan.FromSeconds(5));
        vm.Dispose(); service.Prepared.TrySetResult(); await load;
        vm.Formats.Should().BeEmpty(); vm.Rows.Should().BeEmpty(); vm.CanStart.Should().BeFalse(); service.Starts.Should().Be(0);
    }
    [Fact]
    public async Task Cancelling_preparation_ignores_a_late_success_and_requires_reopening()
    {
        var source = Source(); var service = new Service([source]) { HoldPreparation = true };
        using var vm = new BulkExportViewModel([source], service, Text);
        var load = vm.LoadAsync(); await service.Preparing.Task.WaitAsync(TimeSpan.FromSeconds(5));
        vm.Cancel(); service.Prepared.TrySetResult(); await load;
        vm.Formats.Should().BeEmpty(); vm.IsCancelling.Should().BeFalse(); vm.IsLoading.Should().BeFalse();
        vm.Message.Should().Be("BulkExportCancelled"); vm.CanStart.Should().BeFalse(); vm.CanClose.Should().BeTrue();
    }
    private sealed class QueuedContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<Action> pending = new();
        public override void Post(SendOrPostCallback callback, object? state) => pending.Enqueue(() => callback(state));
        public void Drain() { while (pending.TryDequeue(out var action)) action(); }
    }
    private sealed class Service(IReadOnlyList<BookExportSource> books) : IBulkBookExportService
    {
        public bool Hold, Fail, HoldPreparation; public int Starts;
        public IProgress<BulkExportProgress>? Progress;
        public TaskCompletionSource Preparing { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Prepared { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public BulkExportStatus Status = BulkExportStatus.Exported;
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<IReadOnlyList<BookExportSource>> PrepareAsync(IReadOnlyList<BookExportSource> selection, IProgress<BulkExportProgress>? progress, CancellationToken token)
        {
            if (Fail) throw new IOException("private details");
            Preparing.TrySetResult(); if (HoldPreparation) await Prepared.Task;
            return books;
        }
        public async Task<BulkExportResult> ExportAsync(IReadOnlyList<BookExportSource> preview, EbookFormat format, string destination, IProgress<BulkExportProgress>? progress, CancellationToken token)
        {
            Progress = progress; Starts++; Started.TrySetResult();
            if (Hold) { try { await Task.Delay(Timeout.Infinite, token); } catch (OperationCanceledException) { } }
            return new(preview.Select(book => new BulkExportItem(book.Id, book.Title, "Author", token.IsCancellationRequested ? BulkExportStatus.NotProcessed : Status,
                Status == BulkExportStatus.Exported && !token.IsCancellationRequested ? "copied.epub" : null)).ToArray());
        }
    }
}

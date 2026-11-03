using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using FluentAssertions;

namespace EbookManager.Tests.Books;

public sealed class BulkBookExportServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "saga-bulk-export-" + Guid.NewGuid().ToString("N"));
    private readonly Sources sources = new();
    private BulkBookExportService Service => new(sources, new BookFileExportService(new Store(root)));

    [Fact]
    public async Task Exports_every_selected_format_file_with_unique_names_and_preserves_existing_files()
    {
        var first = Book("Same", EbookFormat.Epub, EbookFormat.Epub);
        var second = Book("Same", EbookFormat.Epub);
        var pdf = Book("PDF only", EbookFormat.Pdf);
        var output = Directory.CreateDirectory(Path.Combine(root, "Out")).FullName;
        File.WriteAllText(Path.Combine(output, "Author - Same.epub"), "existing");
        var preview = await Service.PrepareAsync([first, second, pdf, first], null, default);
        preview.Should().HaveCount(3);
        var result = await Service.ExportAsync(preview, EbookFormat.Epub, output, null, default);
        result.Items.Count(item => item.Status == BulkExportStatus.Exported).Should().Be(3);
        result.Items.Count(item => item.Status == BulkExportStatus.FormatMissing).Should().Be(1);
        result.Items.Where(item => item.Status == BulkExportStatus.Exported).Select(item => item.DestinationPath).Distinct().Should().HaveCount(3);
        File.ReadAllText(Path.Combine(output, "Author - Same.epub")).Should().Be("existing");
        Directory.GetFiles(output, "*.tmp").Should().BeEmpty();
        foreach (var file in first.Files.Concat(second.Files)) File.ReadAllText(Path.Combine(root, file.RelativePath)).Should().Be(file.Id.ToString());
    }

    [Fact]
    public async Task Missing_changed_and_failed_sources_do_not_stop_the_remaining_books()
    {
        var missing = Book("Missing", EbookFormat.Epub); var changed = Book("Changed", EbookFormat.Epub);
        var failure = Book(new string('x', 320), EbookFormat.Epub); var good = Book("Good", EbookFormat.Epub);
        var preview = await Service.PrepareAsync([missing, changed, failure, good], null, default);
        File.Delete(Path.Combine(root, missing.Files[0].RelativePath));
        sources.Rows[changed.Id] = changed with { Title = "New title" };
        var output = Path.Combine(root, "Out");
        var result = await Service.ExportAsync(preview, EbookFormat.Epub, output, null, default);
        result.Items.Select(item => item.Status).Should().Equal(BulkExportStatus.SourceMissing, BulkExportStatus.Changed, BulkExportStatus.Failed, BulkExportStatus.Exported);
        Directory.GetFiles(output).Should().HaveCount(1);
    }

    [Fact]
    public async Task Cancellation_keeps_completed_files_and_reports_remaining_work_without_partial_copies()
    {
        var first = Book("First", EbookFormat.Epub); var second = Book("Second", EbookFormat.Epub);
        var preview = await Service.PrepareAsync([first, second], null, default);
        using var cancellation = new CancellationTokenSource();
        var progress = new Reports(item => { if (!item.Preparing && item.Completed == 1) cancellation.Cancel(); });
        var output = Path.Combine(root, "Out");
        var result = await Service.ExportAsync(preview, EbookFormat.Epub, output, progress, cancellation.Token);
        result.Cancelled.Should().BeTrue();
        result.Items.Select(item => item.Status).Should().Equal(BulkExportStatus.Exported, BulkExportStatus.NotProcessed);
        Directory.GetFiles(output).Should().HaveCount(1);
    }

    [Fact]
    public async Task Preparation_is_bounded_deduplicates_selection_and_retains_missing_book_labels()
    {
        var rows = Enumerable.Range(0, 502).Select(i => new BookExportSource(Guid.NewGuid(), $"Book {i}", ["Author"], [])).ToArray();
        foreach (var row in rows) sources.Rows[row.Id] = row;
        var removed = new BookExportSource(Guid.NewGuid(), "Removed title", ["Author"], []);
        var preview = await Service.PrepareAsync(rows.Concat([removed, rows[0]]).ToArray(), null, default);
        preview.Should().HaveCount(503); preview.Last().Available.Should().BeFalse(); preview.Last().Title.Should().Be(removed.Title);
        sources.Sizes.Should().Equal(500, 3);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await FluentActions.Awaiting(() => Service.PrepareAsync(rows, null, cancelled.Token)).Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Export_revalidates_file_identity_and_handles_missing_books_and_read_failure()
    {
        var first = Book("First", EbookFormat.Epub); var second = Book("Second", EbookFormat.Epub);
        var preview = await Service.PrepareAsync([first, second], null, default);
        sources.Rows[first.Id] = first with { Files = [first.Files[0] with { Sha256 = "changed" }] };
        sources.Rows.Remove(second.Id);
        (await Service.ExportAsync(preview, EbookFormat.Epub, Path.Combine(root, "Out"), null, default)).Items.Select(item => item.Status)
            .Should().Equal(BulkExportStatus.Changed, BulkExportStatus.BookMissing);
        sources.Fail = true;
        (await Service.ExportAsync(preview, EbookFormat.Epub, Path.Combine(root, "Out"), null, default)).Items.Should().OnlyContain(item => item.Status == BulkExportStatus.ReadFailed);
        Directory.Exists(Path.Combine(root, "Out")).Should().BeFalse();
    }

    private BookExportSource Book(string title, params EbookFormat[] formats)
    {
        Directory.CreateDirectory(root); var id = Guid.NewGuid();
        var files = formats.Select(format =>
        {
            var fileId = Guid.NewGuid(); var relative = fileId.ToString("N") + "." + format.ToString().ToLowerInvariant();
            File.WriteAllText(Path.Combine(root, relative), fileId.ToString());
            return new BookFile(fileId, id, format, relative, fileId.ToString(), 36, MetadataWriteBackStatus.Unsupported, null);
        }).ToArray();
        return sources.Rows[id] = new(id, title, ["Author"], files);
    }
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
    private sealed class Reports(Action<BulkExportProgress> report) : IProgress<BulkExportProgress> { public void Report(BulkExportProgress value) => report(value); }
    private sealed class Sources : IBookExportRepository
    {
        public Dictionary<Guid, BookExportSource> Rows { get; } = [];
        public List<int> Sizes { get; } = [];
        public bool Fail;
        public Task<IReadOnlyList<BookExportSource>> ReadAsync(IReadOnlyCollection<Guid> ids, CancellationToken token)
        { token.ThrowIfCancellationRequested(); if (Fail) throw new IOException("Simulated read failure"); Sizes.Add(ids.Count); return Task.FromResult<IReadOnlyList<BookExportSource>>(ids.Where(Rows.ContainsKey).Select(id => Rows[id]).ToArray()); }
    }
    private sealed class Store(string root) : ILibraryFileStore
    {
        public string GetAbsolutePath(string path) => Path.Combine(root, path);
        public Task<(string RelativeBookPath, string? RelativeCoverPath)> CopyIntoLibraryAsync(Guid id, string path, byte[]? cover, CancellationToken token) => throw new NotSupportedException();
        public Task DeleteFileAsync(string path, CancellationToken token) => throw new NotSupportedException();
        public Task DeleteBookDirectoryAsync(Guid id, CancellationToken token) => throw new NotSupportedException();
    }
}

using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;

namespace EbookManager.Application.Books;

public enum BulkExportStatus { Exported, FormatMissing, BookMissing, Changed, SourceMissing, ReadFailed, Failed, NotProcessed }
public sealed record BulkExportProgress(int Completed, int Total, string Title, bool Preparing = false);
public sealed record BulkExportItem(Guid BookId, string Title, string Authors, BulkExportStatus Status, string? DestinationPath = null);
public sealed record BulkExportResult(IReadOnlyList<BulkExportItem> Items)
{ public bool Cancelled => Items.Any(item => item.Status == BulkExportStatus.NotProcessed); }
public interface IBulkBookExportService
{
    Task<IReadOnlyList<BookExportSource>> PrepareAsync(IReadOnlyList<BookExportSource> selection, IProgress<BulkExportProgress>? progress, CancellationToken token);
    Task<BulkExportResult> ExportAsync(IReadOnlyList<BookExportSource> preview, EbookFormat format, string destination, IProgress<BulkExportProgress>? progress, CancellationToken token);
}

public sealed class BulkBookExportService(IBookExportRepository repository, BookFileExportService exporter) : IBulkBookExportService
{
    public async Task<IReadOnlyList<BookExportSource>> PrepareAsync(IReadOnlyList<BookExportSource> selection, IProgress<BulkExportProgress>? progress, CancellationToken token)
    {
        var selected = selection.DistinctBy(book => book.Id).ToArray();
        var found = new Dictionary<Guid, BookExportSource>(); var count = 0;
        foreach (var batch in selected.Chunk(500))
        {
            token.ThrowIfCancellationRequested();
            foreach (var book in await repository.ReadAsync(batch.Select(book => book.Id).ToArray(), token)) found[book.Id] = book;
            count += batch.Length; progress?.Report(new(count, selected.Length, "", true));
        }
        return selected.Select(book => found.GetValueOrDefault(book.Id) ?? book with { Files = [], Available = false }).ToArray();
    }

    private sealed record Work(BookExportSource Book, BookFile? File, BulkExportStatus? Skip);
    public async Task<BulkExportResult> ExportAsync(IReadOnlyList<BookExportSource> preview, EbookFormat format, string destination, IProgress<BulkExportProgress>? progress, CancellationToken token)
    {
        var work = preview.DistinctBy(book => book.Id).SelectMany(book =>
        {
            var files = book.Files.Where(file => file.Format == format).OrderBy(file => file.Id).ToArray();
            return !book.Available ? [new Work(book, null, BulkExportStatus.BookMissing)] : files.Length == 0
                ? [new Work(book, null, BulkExportStatus.FormatMissing)] : files.Select(file => new Work(book, file, null)).ToArray();
        }).ToArray();
        BulkExportItem Item(Work item, BulkExportStatus status, string? path = null) => new(item.Book.Id, item.Book.Title, string.Join(", ", item.Book.Authors), status, path);
        progress?.Report(new(0, work.Length, "", true));
        Dictionary<Guid, BookExportSource> current;
        try { current = (await repository.ReadAsync(work.Where(item => item.Skip is null).Select(item => item.Book.Id).Distinct().ToArray(), token)).ToDictionary(book => book.Id); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { return new(work.Select(item => Item(item, BulkExportStatus.NotProcessed)).ToArray()); }
        catch (Exception) { return new(work.Select(item => Item(item, item.Skip ?? BulkExportStatus.ReadFailed)).ToArray()); }
        var results = new List<BulkExportItem>(); var completed = 0;
        foreach (var item in work)
        {
            if (token.IsCancellationRequested) { results.Add(Item(item, BulkExportStatus.NotProcessed)); continue; }
            if (item.Skip is { } skip) results.Add(Item(item, skip));
            else if (!current.TryGetValue(item.Book.Id, out var book)) results.Add(Item(item, BulkExportStatus.BookMissing));
            else if (book.Title != item.Book.Title || !book.Authors.SequenceEqual(item.Book.Authors) || !book.Files.Any(file =>
                file.Id == item.File!.Id && file.BookId == item.Book.Id && file.RelativePath == item.File.RelativePath && file.Format == item.File.Format && file.Sha256 == item.File.Sha256 && file.SizeBytes == item.File.SizeBytes))
                results.Add(Item(item, BulkExportStatus.Changed));
            else
            {
                try
                {
                    var copied = await exporter.ExportAsync(item.Book, item.File!, destination, token);
                    results.Add(Item(item, copied.Status switch { BookFileExportStatus.Exported => BulkExportStatus.Exported, BookFileExportStatus.SourceMissing => BulkExportStatus.SourceMissing, _ => BulkExportStatus.Failed }, copied.DestinationPath));
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { results.Add(Item(item, BulkExportStatus.NotProcessed)); }
                catch (Exception) { results.Add(Item(item, BulkExportStatus.Failed)); }
            }
            if (results[^1].Status != BulkExportStatus.NotProcessed) progress?.Report(new(++completed, work.Length, item.Book.Title));
        }
        return new(results);
    }
}

using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Series;

namespace EbookManager.Application.Series;

public sealed record SeriesMergeCompletion(SeriesMergeResult Change, IReadOnlyList<SeriesSourceBook> Warnings);
public interface ISeriesMergeService
{
    Task<SeriesMergePreview?> PreviewAsync(IReadOnlyCollection<string> names, CancellationToken token);
    Task<SeriesMergeCompletion> MergeAsync(SeriesMergePreview preview, string targetName, CancellationToken token);
}
public sealed class SeriesMergeService(ISeriesMergeRepository repository, BookService books) : ISeriesMergeService
{
    public Task<SeriesMergePreview?> PreviewAsync(IReadOnlyCollection<string> names, CancellationToken token) => repository.PreviewMergeAsync(names, token);
    public async Task<SeriesMergeCompletion> MergeAsync(SeriesMergePreview preview, string targetName, CancellationToken token)
    {
        var change = await repository.MergeAsync(preview, targetName, token);
        if (change.Status != SeriesMergeStatus.Merged) return new(change, []);
        var warnings = new List<SeriesSourceBook>();
        foreach (var book in change.Books)
        {
            try
            {
                var result = await books.SynchronizeStoredMetadataAsync(book.Id, CancellationToken.None);
                if (result.Status != BookSaveStatus.Succeeded || result.FileResults.Any(file => file.Result.Status == MetadataWriteBackStatus.Failed))
                    warnings.Add(new(book.Id, book.StoredName, book.Title));
            }
            catch (Exception) { warnings.Add(new(book.Id, book.StoredName, book.Title)); }
        }
        return new(change, warnings);
    }
}

using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Series;

namespace EbookManager.Application.Series;

public sealed record SeriesNumberCompletion(SeriesNumberResult Change, bool FileWarning);
public interface ISeriesNumberService
{
    Task<SeriesNumberPreview?> PreviewAsync(Guid bookId, CancellationToken token);
    Task<SeriesNumberCompletion> ChangeAsync(SeriesNumberPreview preview, decimal? number, CancellationToken token);
}
public sealed class SeriesNumberService(ISeriesNumberRepository repository, BookService books) : ISeriesNumberService
{
    public Task<SeriesNumberPreview?> PreviewAsync(Guid bookId, CancellationToken token) => repository.PreviewNumberAsync(bookId, token);
    public async Task<SeriesNumberCompletion> ChangeAsync(SeriesNumberPreview preview, decimal? number, CancellationToken token)
    {
        var change = await repository.ChangeNumberAsync(preview, number, token);
        if (change.Status != SeriesNumberStatus.Changed) return new(change, false);
        // Database commit has completed: synchronize the latest stored metadata despite caller cancellation.
        try
        {
            var result = await books.SynchronizeStoredMetadataAsync(preview.Book.Id, CancellationToken.None);
            return new(change, result.Status != BookSaveStatus.Succeeded || result.FileResults.Any(file => file.Result.Status == MetadataWriteBackStatus.Failed));
        }
        catch (Exception) { return new(change, true); }
    }
}

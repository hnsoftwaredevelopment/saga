using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Series;

namespace EbookManager.Application.Series;

public sealed record SeriesNumbersCompletion(SeriesNumbersResult Change, IReadOnlyList<SeriesSourceBook> Warnings);
public interface ISeriesNumbersService
{
    Task<SeriesNumbersPreview?> PreviewAsync(string name, IReadOnlyList<Guid> selectedIds, CancellationToken token);
    Task<SeriesNumbersCompletion> ChangeAsync(SeriesNumbersPreview preview, IReadOnlyList<SeriesNumberAssignment> assignments, CancellationToken token);
}
public sealed class SeriesNumbersService(ISeriesNumbersRepository repository, BookService books) : ISeriesNumbersService
{
    public Task<SeriesNumbersPreview?> PreviewAsync(string name, IReadOnlyList<Guid> selectedIds, CancellationToken token) => repository.PreviewNumbersAsync(name, selectedIds, token);
    public async Task<SeriesNumbersCompletion> ChangeAsync(SeriesNumbersPreview preview, IReadOnlyList<SeriesNumberAssignment> assignments, CancellationToken token)
    {
        var change = await repository.ChangeNumbersAsync(preview, assignments, token);
        var warnings = new List<SeriesSourceBook>();
        if (change.Status == SeriesNumberStatus.Changed)
        {
            foreach (var book in change.Books)
            {
                // A committed number must reach its sidecar even when the original caller cancels later.
                try
                {
                    var result = await books.SynchronizeStoredMetadataAsync(book.Id, CancellationToken.None);
                    if (result.Status != BookSaveStatus.Succeeded || result.FileResults.Any(file => file.Result.Status == MetadataWriteBackStatus.Failed)) warnings.Add(book);
                }
                catch (Exception) { warnings.Add(book); }
            }
        }
        return new(change, warnings);
    }
}

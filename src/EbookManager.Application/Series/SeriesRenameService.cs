using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Series;

namespace EbookManager.Application.Series;

public sealed record SeriesRenameCompletion(SeriesRenameResult Change, IReadOnlyList<SeriesSourceBook> Warnings);

public interface ISeriesRenameService
{
    Task<SeriesRenamePreview> PreviewAsync(string name, CancellationToken token);
    Task<SeriesRenameCompletion> RenameAsync(SeriesRenamePreview preview, string newName, CancellationToken token);
}

public sealed class SeriesRenameService(ISeriesRepository repository, BookService books) : ISeriesRenameService
{
    public Task<SeriesRenamePreview> PreviewAsync(string name, CancellationToken token) => repository.PreviewRenameAsync(name, token);

    public async Task<SeriesRenameCompletion> RenameAsync(SeriesRenamePreview preview, string newName, CancellationToken token)
    {
        var change = await repository.RenameAsync(preview, newName, token);
        if (change.Status != SeriesRenameStatus.Renamed) return new(change, []);
        var warnings = new List<SeriesSourceBook>();
        // Commit has completed: finish synchronizing current stored metadata, even after cancellation.
        foreach (var book in change.Books)
        {
            try
            {
                var result = await books.SynchronizeStoredMetadataAsync(book.Id, CancellationToken.None);
                if (result.Status != BookSaveStatus.Succeeded ||
                    result.FileResults.Any(file => file.Result.Status == MetadataWriteBackStatus.Failed)) warnings.Add(book);
            }
            catch (Exception) { warnings.Add(book); }
        }
        return new(change, warnings.AsReadOnly());
    }
}

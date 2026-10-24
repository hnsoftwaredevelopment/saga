using EbookManager.Domain.Series;

namespace EbookManager.Domain.Abstractions;

public interface ISeriesRepository
{
    Task<SeriesPage> SearchAsync(string query, int skip, int take, CancellationToken token);
    Task<SeriesBookPage> ListBooksAsync(string name, int skip, int take, CancellationToken token);
    Task<SeriesRenamePreview> PreviewRenameAsync(string name, CancellationToken token);
    Task<SeriesRenameResult> RenameAsync(SeriesRenamePreview preview, string newName, CancellationToken token);
}

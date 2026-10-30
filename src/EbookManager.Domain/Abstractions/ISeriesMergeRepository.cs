using EbookManager.Domain.Series;

namespace EbookManager.Domain.Abstractions;

public interface ISeriesMergeRepository
{
    Task<SeriesMergePreview?> PreviewMergeAsync(IReadOnlyCollection<string> names, CancellationToken token);
    Task<SeriesMergeResult> MergeAsync(SeriesMergePreview preview, string targetName, CancellationToken token);
}

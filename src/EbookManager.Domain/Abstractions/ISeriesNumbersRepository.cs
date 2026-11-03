using EbookManager.Domain.Series;

namespace EbookManager.Domain.Abstractions;

public interface ISeriesNumbersRepository
{
    Task<SeriesNumbersPreview?> PreviewNumbersAsync(string name, IReadOnlyList<Guid> selectedIds, CancellationToken token);
    Task<SeriesNumbersResult> ChangeNumbersAsync(SeriesNumbersPreview preview, IReadOnlyList<SeriesNumberAssignment> assignments, CancellationToken token);
}

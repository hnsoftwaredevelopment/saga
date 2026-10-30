using EbookManager.Domain.Series;

namespace EbookManager.Domain.Abstractions;

public interface ISeriesNumberRepository
{
    Task<SeriesNumberPreview?> PreviewNumberAsync(Guid bookId, CancellationToken token);
    Task<SeriesNumberResult> ChangeNumberAsync(SeriesNumberPreview preview, decimal? number, CancellationToken token);
}

using EbookManager.Domain.Metadata;

namespace EbookManager.Domain.Abstractions;

public interface IMetadataQualityExclusionRepository
{
    Task<IReadOnlySet<MetadataQualityExclusionKey>> ListMetadataQualityExclusionsAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MetadataQualityExclusion>> ListMetadataQualityExclusionDetailsAsync(
        CancellationToken cancellationToken);

    Task AddMetadataQualityExclusionsAsync(
        IReadOnlyCollection<MetadataQualityExclusionKey> keys,
        CancellationToken cancellationToken);

    Task RemoveMetadataQualityExclusionsAsync(
        IReadOnlyCollection<MetadataQualityExclusionKey> keys,
        CancellationToken cancellationToken);

    Task ClearMetadataQualityExclusionsAsync(CancellationToken cancellationToken);
}

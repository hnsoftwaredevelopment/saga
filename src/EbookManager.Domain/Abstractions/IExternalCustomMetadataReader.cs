using EbookManager.Domain.CustomMetadata;

namespace EbookManager.Domain.Abstractions;

public interface IExternalCustomMetadataReader
{
    Task<IReadOnlyList<ExternalCustomMetadataValue>> ReadAsync(
        string sourcePath,
        CancellationToken cancellationToken);
}

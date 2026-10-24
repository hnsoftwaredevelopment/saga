using EbookManager.Domain.Books;

namespace EbookManager.Domain.Abstractions;

public interface IBookMetadataSnapshotRepository
{
    /// <summary>Reads the requested existing books without cover blobs; missing IDs are omitted.</summary>
    Task<IReadOnlyList<Book>> ListMetadataAsync(IReadOnlyCollection<Guid> bookIds, CancellationToken cancellationToken);
}

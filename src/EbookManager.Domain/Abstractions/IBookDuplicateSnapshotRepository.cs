using EbookManager.Domain.Books;

namespace EbookManager.Domain.Abstractions;

public interface IBookDuplicateSnapshotRepository
{
    Task<BookDuplicateSnapshot> CreateDuplicateSnapshotAsync(CancellationToken cancellationToken);
}

using EbookManager.Domain.Books;

namespace EbookManager.Domain.Abstractions;

public interface IBookExportRepository
{
    Task<IReadOnlyList<BookExportSource>> ReadAsync(IReadOnlyCollection<Guid> ids, CancellationToken token);
}

using EbookManager.Domain.Books;

namespace EbookManager.Domain.Abstractions;

public interface IBookPagedRepository
{
    Task<int> CountAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Book>> ListPageAsync(
        int skip,
        int take,
        CancellationToken cancellationToken);
}

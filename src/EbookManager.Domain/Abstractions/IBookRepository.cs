using EbookManager.Domain.Books;

namespace EbookManager.Domain.Abstractions;

public interface IBookRepository
{
    Task<IReadOnlyList<Book>> ListAsync(CancellationToken cancellationToken);
    Task<Book?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> HasHashAsync(string sha256, CancellationToken cancellationToken);
    Task<bool> HasNormalizedTitleAndAuthorAsync(
        string title,
        IReadOnlyList<string> authors,
        CancellationToken cancellationToken);
    Task AddAsync(Book book, BookFile file, CancellationToken cancellationToken);
    Task UpdateAsync(Book book, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

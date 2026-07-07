using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;

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
    Task<Book?> FindByNormalizedTitleAndAuthorAsync(
        string title,
        IReadOnlyList<string> authors,
        CancellationToken cancellationToken) =>
        Task.FromResult<Book?>(null);
    Task<IReadOnlyList<Book>> FindByNormalizedTitleAsync(
        string title,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Book>>([]);
    Task AddAsync(Book book, BookFile file, CancellationToken cancellationToken);
    Task AddFileAsync(BookFile file, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Adding files to existing books is not supported by this repository.");
    Task UpdateAsync(Book book, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<BookFile>> ListFilesAsync(Guid bookId, CancellationToken cancellationToken);
    Task UpdateFileWriteBackAsync(
        Guid fileId,
        MetadataWriteResult result,
        CancellationToken cancellationToken);
}

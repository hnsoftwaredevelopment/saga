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
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Book>> FindByNormalizedTitleAsync(
        string title,
        CancellationToken cancellationToken);
    Task AddAsync(Book book, BookFile file, CancellationToken cancellationToken);
    Task AddFileAsync(BookFile file, CancellationToken cancellationToken);
    Task AttachFilesToBookAsync(Guid sourceBookId, Guid targetBookId, CancellationToken cancellationToken);
    Task UpdateAsync(Book book, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<BookFileDeleteRepositoryResult> DeleteFileAsync(Guid bookId, Guid fileId, CancellationToken cancellationToken);
    Task<IReadOnlyList<BookFile>> ListFilesAsync(Guid bookId, CancellationToken cancellationToken);
    Task UpdateFileWriteBackAsync(
        Guid fileId,
        MetadataWriteResult result,
        CancellationToken cancellationToken);
}

public enum BookFileDeleteRepositoryStatus
{
    Deleted,
    LastFormat,
    NotFound
}

public sealed record BookFileDeleteRepositoryResult(BookFileDeleteRepositoryStatus Status);

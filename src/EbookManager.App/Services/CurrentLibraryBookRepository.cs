using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using EbookManager.Infrastructure.Persistence;
using EbookManager.Infrastructure.Persistence.Repositories;
using EbookManager.Libraries;

namespace EbookManager.App.Services;

public sealed class CurrentLibraryBookRepository(
    CurrentLibrary currentLibrary,
    LibraryDbContextFactory contextFactory)
    : IBookRepository, IBookDuplicateSnapshotRepository, IBookPagedRepository, IBookBulkMetadataRepository
{
    public Task<IReadOnlyList<Book>> ListAsync(CancellationToken cancellationToken)
    {
        var repository = TryCreateRepository();
        return repository is null
            ? Task.FromResult<IReadOnlyList<Book>>([])
            : repository.ListAsync(cancellationToken);
    }

    public Task<Book?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var repository = TryCreateRepository();
        return repository is null
            ? Task.FromResult<Book?>(null)
            : repository.GetAsync(id, cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        var repository = TryCreateRepository();
        return repository is null
            ? Task.FromResult(0)
            : repository.CountAsync(cancellationToken);
    }

    public Task<IReadOnlyList<Book>> ListPageAsync(
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var repository = TryCreateRepository();
        return repository is null
            ? Task.FromResult<IReadOnlyList<Book>>([])
            : repository.ListPageAsync(skip, take, cancellationToken);
    }

    public Task<bool> HasHashAsync(string sha256, CancellationToken cancellationToken) =>
        CreateRepository().HasHashAsync(sha256, cancellationToken);

    public Task<bool> HasNormalizedTitleAndAuthorAsync(
        string title,
        IReadOnlyList<string> authors,
        CancellationToken cancellationToken) =>
        CreateRepository().HasNormalizedTitleAndAuthorAsync(title, authors, cancellationToken);

    public Task<Book?> FindByNormalizedTitleAndAuthorAsync(
        string title,
        IReadOnlyList<string> authors,
        CancellationToken cancellationToken) =>
        CreateRepository().FindByNormalizedTitleAndAuthorAsync(title, authors, cancellationToken);

    public Task<IReadOnlyList<Book>> FindByNormalizedTitleAsync(
        string title,
        CancellationToken cancellationToken) =>
        CreateRepository().FindByNormalizedTitleAsync(title, cancellationToken);

    public Task<BookDuplicateSnapshot> CreateDuplicateSnapshotAsync(CancellationToken cancellationToken)
    {
        var repository = TryCreateRepository();
        return repository is null
            ? Task.FromResult(new BookDuplicateSnapshot(
                new HashSet<string>(StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal)))
            : repository.CreateDuplicateSnapshotAsync(cancellationToken);
    }

    public Task AddAsync(Book book, BookFile file, CancellationToken cancellationToken) =>
        CreateRepository().AddAsync(book, file, cancellationToken);

    public Task AddFileAsync(BookFile file, CancellationToken cancellationToken) =>
        CreateRepository().AddFileAsync(file, cancellationToken);

    public Task AttachFilesToBookAsync(Guid sourceBookId, Guid targetBookId, CancellationToken cancellationToken) =>
        CreateRepository().AttachFilesToBookAsync(sourceBookId, targetBookId, cancellationToken);

    public Task UpdateAsync(Book book, CancellationToken cancellationToken) =>
        CreateRepository().UpdateAsync(book, cancellationToken);

    public Task<int> UpdateScalarMetadataAsync(
        IReadOnlyCollection<Guid> bookIds,
        BookScalarMetadataField field,
        string? value,
        CancellationToken cancellationToken)
    {
        var repository = TryCreateRepository();
        return repository is null
            ? Task.FromResult(0)
            : repository.UpdateScalarMetadataAsync(bookIds, field, value, cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        CreateRepository().DeleteAsync(id, cancellationToken);

    public Task DeleteFileAsync(Guid fileId, CancellationToken cancellationToken) =>
        CreateRepository().DeleteFileAsync(fileId, cancellationToken);

    public Task<IReadOnlyList<BookFile>> ListFilesAsync(Guid bookId, CancellationToken cancellationToken)
    {
        var repository = TryCreateRepository();
        return repository is null
            ? Task.FromResult<IReadOnlyList<BookFile>>([])
            : repository.ListFilesAsync(bookId, cancellationToken);
    }

    public Task UpdateFileWriteBackAsync(
        Guid fileId,
        MetadataWriteResult result,
        CancellationToken cancellationToken) =>
        CreateRepository().UpdateFileWriteBackAsync(fileId, result, cancellationToken);

    private EfBookRepository CreateRepository() =>
        TryCreateRepository() ?? throw new InvalidOperationException("No active library is loaded.");

    private EfBookRepository? TryCreateRepository()
    {
        var library = currentLibrary.Current;
        return library is null ? null : new EfBookRepository(contextFactory, library.DirectoryPath);
    }
}

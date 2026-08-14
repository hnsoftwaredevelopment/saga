using EbookManager.Domain.Books;

namespace EbookManager.Domain.Abstractions;

public interface IBookBulkMetadataRepository
{
    Task<int> UpdateScalarMetadataAsync(
        IReadOnlyCollection<Guid> bookIds,
        BookScalarMetadataField field,
        string? value,
        CancellationToken cancellationToken);

    Task<int> UpdateListMetadataAsync(
        IReadOnlyCollection<Book> books,
        BookListMetadataField field,
        CancellationToken cancellationToken);
}

public enum BookScalarMetadataField
{
    Series,
    Language
}

public enum BookListMetadataField
{
    Authors,
    Tags
}

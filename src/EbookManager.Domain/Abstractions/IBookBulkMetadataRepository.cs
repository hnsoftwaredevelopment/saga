namespace EbookManager.Domain.Abstractions;

public interface IBookBulkMetadataRepository
{
    Task<int> UpdateScalarMetadataAsync(
        IReadOnlyCollection<Guid> bookIds,
        BookScalarMetadataField field,
        string? value,
        CancellationToken cancellationToken);
}

public enum BookScalarMetadataField
{
    Series,
    Language
}

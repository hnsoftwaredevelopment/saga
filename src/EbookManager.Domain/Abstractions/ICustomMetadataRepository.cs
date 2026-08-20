using EbookManager.Domain.CustomMetadata;

namespace EbookManager.Domain.Abstractions;

public interface ICustomMetadataRepository
{
    Task<IReadOnlyList<CustomMetadataFieldDefinition>> ListDefinitionsAsync(CancellationToken cancellationToken);
    Task<CustomMetadataFieldDefinition> AddDefinitionAsync(
        string name,
        CustomMetadataFieldType type,
        CancellationToken cancellationToken);
    Task RenameDefinitionAsync(Guid fieldId, string name, CancellationToken cancellationToken);
    Task UpdateDefinitionOptionsAsync(
        Guid fieldId,
        IReadOnlyList<string> options,
        CancellationToken cancellationToken);
    Task DeleteDefinitionAsync(Guid fieldId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CustomMetadataValue>> GetValuesAsync(Guid bookId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CustomMetadataValue>> GetValuesForBooksAsync(
        IReadOnlyCollection<Guid> bookIds,
        CancellationToken cancellationToken);
    Task SetValueAsync(CustomMetadataValue value, CancellationToken cancellationToken);
    Task DeleteValueAsync(Guid bookId, Guid fieldId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> CleanupFilterValueAsync(
        Guid fieldId,
        string oldValue,
        string? replacementValue,
        bool remove,
        CancellationToken cancellationToken);
}

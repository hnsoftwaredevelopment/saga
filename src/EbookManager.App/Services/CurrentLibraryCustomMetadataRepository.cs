using EbookManager.Domain.Abstractions;
using EbookManager.Domain.CustomMetadata;
using EbookManager.Infrastructure.Persistence;
using EbookManager.Infrastructure.Persistence.Repositories;
using EbookManager.Libraries;

namespace EbookManager.App.Services;

public sealed class CurrentLibraryCustomMetadataRepository(
    CurrentLibrary currentLibrary,
    LibraryDbContextFactory contextFactory) : ICustomMetadataRepository
{
    public Task<IReadOnlyList<CustomMetadataFieldDefinition>> ListDefinitionsAsync(CancellationToken cancellationToken)
    {
        var repository = TryCreateRepository();
        return repository is null
            ? Task.FromResult<IReadOnlyList<CustomMetadataFieldDefinition>>([])
            : repository.ListDefinitionsAsync(cancellationToken);
    }

    public Task<CustomMetadataFieldDefinition> AddDefinitionAsync(
        string name,
        CustomMetadataFieldType type,
        CancellationToken cancellationToken) =>
        CreateRepository().AddDefinitionAsync(name, type, cancellationToken);

    public Task RenameDefinitionAsync(Guid fieldId, string name, CancellationToken cancellationToken) =>
        CreateRepository().RenameDefinitionAsync(fieldId, name, cancellationToken);

    public Task UpdateDefinitionOptionsAsync(
        Guid fieldId,
        IReadOnlyList<string> options,
        CancellationToken cancellationToken) =>
        CreateRepository().UpdateDefinitionOptionsAsync(fieldId, options, cancellationToken);

    public Task DeleteDefinitionAsync(Guid fieldId, CancellationToken cancellationToken) =>
        CreateRepository().DeleteDefinitionAsync(fieldId, cancellationToken);

    public Task<IReadOnlyList<CustomMetadataValue>> GetValuesAsync(Guid bookId, CancellationToken cancellationToken)
    {
        var repository = TryCreateRepository();
        return repository is null
            ? Task.FromResult<IReadOnlyList<CustomMetadataValue>>([])
            : repository.GetValuesAsync(bookId, cancellationToken);
    }

    public Task<IReadOnlyList<CustomMetadataValue>> GetValuesForBooksAsync(
        IReadOnlyCollection<Guid> bookIds,
        CancellationToken cancellationToken)
    {
        var repository = TryCreateRepository();
        return repository is null
            ? Task.FromResult<IReadOnlyList<CustomMetadataValue>>([])
            : repository.GetValuesForBooksAsync(bookIds, cancellationToken);
    }

    public Task SetValueAsync(CustomMetadataValue value, CancellationToken cancellationToken) =>
        CreateRepository().SetValueAsync(value, cancellationToken);

    public Task DeleteValueAsync(Guid bookId, Guid fieldId, CancellationToken cancellationToken) =>
        CreateRepository().DeleteValueAsync(bookId, fieldId, cancellationToken);

    public Task<IReadOnlyList<Guid>> CleanupFilterValueAsync(
        Guid fieldId,
        string oldValue,
        string? replacementValue,
        bool remove,
        CancellationToken cancellationToken)
    {
        var repository = TryCreateRepository();
        return repository is null
            ? Task.FromResult<IReadOnlyList<Guid>>([])
            : repository.CleanupFilterValueAsync(fieldId, oldValue, replacementValue, remove, cancellationToken);
    }

    private EfCustomMetadataRepository CreateRepository() =>
        TryCreateRepository() ?? throw new InvalidOperationException("No active library is loaded.");

    private EfCustomMetadataRepository? TryCreateRepository()
    {
        var library = currentLibrary.Current;
        return library is null ? null : new EfCustomMetadataRepository(contextFactory, library.DirectoryPath);
    }
}

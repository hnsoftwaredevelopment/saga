using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Libraries;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Infrastructure.Persistence;

public sealed class LibraryDatabaseInitializer(
    LibraryDbContextFactory contextFactory,
    ILibraryStorageMigrator storageMigrator) : ILibraryDatabaseInitializer
{
    public Task InitializeAsync(LibraryDescriptor library, CancellationToken cancellationToken) =>
        InitializeAsync(library, progress: null, cancellationToken);

    public async Task InitializeAsync(
        LibraryDescriptor library,
        IProgress<LibraryStorageMigrationProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(library);

        await using var context = contextFactory.Create(library.DirectoryPath);
        await context.Database.MigrateAsync(cancellationToken);
        await Task.Run(
            () => storageMigrator.MigrateAsync(library, progress, cancellationToken),
            cancellationToken);
    }
}

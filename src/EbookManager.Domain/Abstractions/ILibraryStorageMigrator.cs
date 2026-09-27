using EbookManager.Domain.Libraries;

namespace EbookManager.Domain.Abstractions;

public interface ILibraryStorageMigrator
{
    Task<LibraryStorageMigrationResult> MigrateAsync(
        LibraryDescriptor library,
        IProgress<LibraryStorageMigrationProgress>? progress,
        CancellationToken cancellationToken);
}

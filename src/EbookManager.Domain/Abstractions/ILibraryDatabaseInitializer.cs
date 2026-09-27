using EbookManager.Domain.Libraries;

namespace EbookManager.Domain.Abstractions;

public interface ILibraryDatabaseInitializer
{
    Task InitializeAsync(LibraryDescriptor library, CancellationToken cancellationToken);

    Task InitializeAsync(
        LibraryDescriptor library,
        IProgress<LibraryStorageMigrationProgress>? progress,
        CancellationToken cancellationToken) =>
        InitializeAsync(library, cancellationToken);
}

using EbookManager.Domain.Libraries;

namespace EbookManager.Domain.Abstractions;

public interface ILibraryDatabaseInitializer
{
    Task InitializeAsync(LibraryDescriptor library, CancellationToken cancellationToken);
}

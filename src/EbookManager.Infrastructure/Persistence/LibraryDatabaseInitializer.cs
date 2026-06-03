using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Libraries;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Infrastructure.Persistence;

public sealed class LibraryDatabaseInitializer(LibraryDbContextFactory contextFactory) : ILibraryDatabaseInitializer
{
    public async Task InitializeAsync(LibraryDescriptor library, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(library);

        await using var context = contextFactory.Create(library.DirectoryPath);
        await context.Database.MigrateAsync(cancellationToken);
    }
}

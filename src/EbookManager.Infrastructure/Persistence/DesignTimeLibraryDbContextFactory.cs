using Microsoft.EntityFrameworkCore.Design;

namespace EbookManager.Infrastructure.Persistence;

public sealed class DesignTimeLibraryDbContextFactory : IDesignTimeDbContextFactory<LibraryDbContext>
{
    public LibraryDbContext CreateDbContext(string[] args)
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "EbookManager.DesignTime");
        Directory.CreateDirectory(directoryPath);
        return new LibraryDbContextFactory().Create(directoryPath);
    }
}

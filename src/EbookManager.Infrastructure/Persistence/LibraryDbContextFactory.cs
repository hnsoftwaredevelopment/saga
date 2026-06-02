using Microsoft.EntityFrameworkCore;

namespace EbookManager.Infrastructure.Persistence;

public sealed class LibraryDbContextFactory
{
    public LibraryDbContext Create(string directoryPath)
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseSqlite($"Data Source={Path.Combine(directoryPath, "library.db")}")
            .Options;
        return new LibraryDbContext(options);
    }
}

using EbookManager.Domain.Books;
using EbookManager.Domain.Libraries;
using EbookManager.Domain.Metadata;
using EbookManager.Infrastructure.Files;
using EbookManager.Infrastructure.Persistence;
using EbookManager.Infrastructure.Persistence.Repositories;
using EbookManager.Tests.TestSupport;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Tests.Infrastructure;

public sealed class LibraryStorageMigratorTests
{
    [Fact]
    public async Task Migration_moves_a_legacy_book_and_updates_all_relative_paths()
    {
        using var library = new TemporaryLibrary();
        var (factory, bookId) = await CreateLegacyBookAsync(library.DirectoryPath, includeCover: true);
        var layout = new ManagedLibraryStorageLayout(library.DirectoryPath);
        var legacyDirectory = layout.GetLegacyBookDirectory(bookId);
        await File.WriteAllBytesAsync(Path.Combine(legacyDirectory, "book.epub"), [1, 2, 3]);
        await File.WriteAllBytesAsync(Path.Combine(legacyDirectory, "cover.jpg"), [4, 5, 6]);
        var progress = new List<LibraryStorageMigrationProgress>();
        var migrator = new LibraryStorageMigrator(factory);

        var result = await migrator.MigrateAsync(
            new LibraryDescriptor("Test", library.DirectoryPath, DateTimeOffset.UtcNow),
            new SynchronousProgress<LibraryStorageMigrationProgress>(progress.Add),
            default);

        result.Status.Should().Be(LibraryStorageMigrationStatus.Completed);
        result.MigratedCount.Should().Be(1);
        Directory.Exists(legacyDirectory).Should().BeFalse();
        Directory.Exists(layout.GetBookDirectory(bookId)).Should().BeTrue();
        progress.Should().ContainSingle().Which.ProcessedCount.Should().Be(1);

        await using var context = factory.Create(library.DirectoryPath);
        var id = bookId.ToString("N");
        (await context.BookFiles.SingleAsync()).RelativePath
            .Should().Be($"books/{id[..2]}/{id}/book.epub");
        (await context.Books.SingleAsync()).CoverRelativePath
            .Should().Be($"books/{id[..2]}/{id}/cover.jpg");
        File.Exists(Path.Combine(
            library.DirectoryPath,
            "backups",
            LibraryStorageMigrator.DatabaseBackupFileName)).Should().BeTrue();
    }

    [Fact]
    public async Task Migration_resumes_after_the_directory_moved_before_database_update()
    {
        using var library = new TemporaryLibrary();
        var (factory, bookId) = await CreateLegacyBookAsync(library.DirectoryPath, includeCover: false);
        var layout = new ManagedLibraryStorageLayout(library.DirectoryPath);
        await File.WriteAllBytesAsync(Path.Combine(layout.GetLegacyBookDirectory(bookId), "book.epub"), [1]);
        Directory.CreateDirectory(Path.GetDirectoryName(layout.GetBookDirectory(bookId))!);
        Directory.Move(layout.GetLegacyBookDirectory(bookId), layout.GetBookDirectory(bookId));
        var migrator = new LibraryStorageMigrator(factory);

        var result = await migrator.MigrateAsync(
            new LibraryDescriptor("Test", library.DirectoryPath, DateTimeOffset.UtcNow),
            progress: null,
            default);

        result.Status.Should().Be(LibraryStorageMigrationStatus.Completed);
        result.MigratedCount.Should().Be(1);
        await using var context = factory.Create(library.DirectoryPath);
        var id = bookId.ToString("N");
        (await context.BookFiles.SingleAsync()).RelativePath
            .Should().Be($"books/{id[..2]}/{id}/book.epub");
    }

    [Fact]
    public async Task Migration_refuses_old_and_new_directories_for_the_same_book()
    {
        using var library = new TemporaryLibrary();
        var (factory, bookId) = await CreateLegacyBookAsync(library.DirectoryPath, includeCover: false);
        var layout = new ManagedLibraryStorageLayout(library.DirectoryPath);
        await File.WriteAllBytesAsync(Path.Combine(layout.GetLegacyBookDirectory(bookId), "book.epub"), [1]);
        Directory.CreateDirectory(layout.GetBookDirectory(bookId));
        var migrator = new LibraryStorageMigrator(factory);

        var action = () => migrator.MigrateAsync(
            new LibraryDescriptor("Test", library.DirectoryPath, DateTimeOffset.UtcNow),
            progress: null,
            default);

        (await action.Should().ThrowAsync<LibraryStorageMigrationException>())
            .Which.BookId.Should().Be(bookId);
        Directory.Exists(layout.GetLegacyBookDirectory(bookId)).Should().BeTrue();
        Directory.Exists(layout.GetBookDirectory(bookId)).Should().BeTrue();
    }

    [Fact]
    public async Task Migration_refuses_missing_storage_when_database_still_has_legacy_paths()
    {
        using var library = new TemporaryLibrary();
        var (factory, bookId) = await CreateLegacyBookAsync(
            library.DirectoryPath,
            includeCover: false,
            createLegacyDirectory: false);
        var migrator = new LibraryStorageMigrator(factory);

        var action = () => migrator.MigrateAsync(
            new LibraryDescriptor("Test", library.DirectoryPath, DateTimeOffset.UtcNow),
            progress: null,
            default);

        (await action.Should().ThrowAsync<LibraryStorageMigrationException>())
            .Which.BookId.Should().Be(bookId);
    }

    [Fact]
    public async Task Migration_leaves_unknown_directories_untouched()
    {
        using var library = new TemporaryLibrary();
        var factory = new LibraryDbContextFactory();
        await using (var context = factory.Create(library.DirectoryPath))
        {
            await context.Database.MigrateAsync();
        }

        var unknownDirectory = Path.Combine(library.DirectoryPath, "books", "not-a-book");
        Directory.CreateDirectory(unknownDirectory);
        await File.WriteAllTextAsync(Path.Combine(unknownDirectory, "keep.txt"), "keep");
        var migrator = new LibraryStorageMigrator(factory);

        var result = await migrator.MigrateAsync(
            new LibraryDescriptor("Test", library.DirectoryPath, DateTimeOffset.UtcNow),
            progress: null,
            default);

        result.Status.Should().Be(LibraryStorageMigrationStatus.NotRequired);
        File.Exists(Path.Combine(unknownDirectory, "keep.txt")).Should().BeTrue();
        Directory.Exists(Path.Combine(library.DirectoryPath, "backups")).Should().BeFalse();
    }

    [Fact]
    public async Task Migration_skips_a_book_that_is_already_fully_sharded()
    {
        using var library = new TemporaryLibrary();
        var (factory, bookId) = await CreateLegacyBookAsync(library.DirectoryPath, includeCover: false);
        var layout = new ManagedLibraryStorageLayout(library.DirectoryPath);
        await File.WriteAllBytesAsync(Path.Combine(layout.GetLegacyBookDirectory(bookId), "book.epub"), [1, 2, 3]);
        Directory.CreateDirectory(Path.GetDirectoryName(layout.GetBookDirectory(bookId))!);
        Directory.Move(layout.GetLegacyBookDirectory(bookId), layout.GetBookDirectory(bookId));
        await using (var context = factory.Create(library.DirectoryPath))
        {
            var file = await context.BookFiles.SingleAsync();
            var id = bookId.ToString("N");
            file.RelativePath = $"books/{id[..2]}/{id}/book.epub";
            await context.SaveChangesAsync();
        }

        var result = await new LibraryStorageMigrator(factory).MigrateAsync(
            new LibraryDescriptor("Test", library.DirectoryPath, DateTimeOffset.UtcNow),
            progress: null,
            default);

        result.Status.Should().Be(LibraryStorageMigrationStatus.NotRequired);
        result.MigratedCount.Should().Be(0);
        Directory.Exists(Path.Combine(library.DirectoryPath, "backups")).Should().BeFalse();
    }

    private static async Task<(LibraryDbContextFactory Factory, Guid BookId)> CreateLegacyBookAsync(
        string libraryPath,
        bool includeCover,
        bool createLegacyDirectory = true)
    {
        var factory = new LibraryDbContextFactory();
        await using (var context = factory.Create(libraryPath))
        {
            await context.Database.MigrateAsync();
        }

        var bookId = Guid.NewGuid();
        var id = bookId.ToString("N");
        var now = DateTimeOffset.UtcNow;
        var book = new Book(
            bookId,
            new BookMetadata("Legacy book", ["Author"]),
            ReadingStatus.Unread,
            includeCover ? $"books/{id}/cover.jpg" : null,
            now,
            now);
        var file = new BookFile(
            Guid.NewGuid(),
            bookId,
            EbookFormat.Epub,
            $"books/{id}/book.epub",
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bookId.ToByteArray())),
            3,
            MetadataWriteBackStatus.NotAttempted,
            null);
        await new EfBookRepository(factory, libraryPath).AddAsync(book, file, default);

        if (createLegacyDirectory)
        {
            Directory.CreateDirectory(new ManagedLibraryStorageLayout(libraryPath).GetLegacyBookDirectory(bookId));
        }

        return (factory, bookId);
    }

    private sealed class SynchronousProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }

    private sealed class TemporaryLibrary : IDisposable
    {
        private readonly TemporaryDirectory temporaryDirectory = new();

        public string DirectoryPath => temporaryDirectory.DirectoryPath;

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            temporaryDirectory.Dispose();
        }
    }
}

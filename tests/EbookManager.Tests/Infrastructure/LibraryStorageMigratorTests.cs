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
        progress.Should().HaveCount(3);
        progress[0].Should().Be(new LibraryStorageMigrationProgress(0, 0, null));
        progress[1].Should().Be(new LibraryStorageMigrationProgress(1, 0, null));
        progress[2].Should().Be(new LibraryStorageMigrationProgress(1, 1, bookId));

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
            .Which.StorageId.Should().Be(bookId);
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
            .Which.StorageId.Should().Be(bookId);
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

    [Fact]
    public async Task Migration_refuses_an_invalid_existing_database_backup_before_moving_books()
    {
        using var library = new TemporaryLibrary();
        var (factory, bookId) = await CreateLegacyBookAsync(library.DirectoryPath, includeCover: false);
        var layout = new ManagedLibraryStorageLayout(library.DirectoryPath);
        await File.WriteAllBytesAsync(Path.Combine(layout.GetLegacyBookDirectory(bookId), "book.epub"), [1, 2, 3]);
        var backupDirectory = Directory.CreateDirectory(Path.Combine(library.DirectoryPath, "backups"));
        await File.WriteAllTextAsync(
            Path.Combine(backupDirectory.FullName, LibraryStorageMigrator.DatabaseBackupFileName),
            "not a database");

        var action = () => new LibraryStorageMigrator(factory).MigrateAsync(
            new LibraryDescriptor("Test", library.DirectoryPath, DateTimeOffset.UtcNow),
            progress: null,
            default);

        var exception = await action.Should().ThrowAsync<LibraryStorageMigrationException>();
        exception.Which.StorageId.Should().BeNull();
        Directory.Exists(layout.GetLegacyBookDirectory(bookId)).Should().BeTrue();
        Directory.Exists(layout.GetBookDirectory(bookId)).Should().BeFalse();
    }

    [Fact]
    public async Task Migration_preserves_a_referenced_storage_directory_from_a_merged_book()
    {
        using var library = new TemporaryLibrary();
        var (factory, bookId) = await CreateLegacyBookAsync(
            library.DirectoryPath,
            includeCover: true,
            createLegacyDirectory: false);
        var storageId = Guid.NewGuid();
        var layout = new ManagedLibraryStorageLayout(library.DirectoryPath);
        var legacyStorageDirectory = layout.GetLegacyBookDirectory(storageId);
        Directory.CreateDirectory(legacyStorageDirectory);
        await File.WriteAllBytesAsync(Path.Combine(legacyStorageDirectory, "book.epub"), [1, 2, 3]);
        await File.WriteAllBytesAsync(Path.Combine(legacyStorageDirectory, "cover.jpg"), [4, 5, 6]);
        await using (var context = factory.Create(library.DirectoryPath))
        {
            var id = storageId.ToString("N");
            (await context.BookFiles.SingleAsync()).RelativePath = $"books/{id}/book.epub";
            (await context.Books.SingleAsync()).CoverRelativePath = $"books/{id}/cover.jpg";
            await context.SaveChangesAsync();
        }

        var result = await new LibraryStorageMigrator(factory).MigrateAsync(
            new LibraryDescriptor("Test", library.DirectoryPath, DateTimeOffset.UtcNow),
            progress: null,
            default);

        result.MigratedCount.Should().Be(1);
        Directory.Exists(legacyStorageDirectory).Should().BeFalse();
        Directory.Exists(layout.GetBookDirectory(storageId)).Should().BeTrue();
        await using var verifyContext = factory.Create(library.DirectoryPath);
        var shardedId = storageId.ToString("N");
        (await verifyContext.BookFiles.SingleAsync()).RelativePath
            .Should().Be($"books/{shardedId[..2]}/{shardedId}/book.epub");
        (await verifyContext.Books.SingleAsync()).CoverRelativePath
            .Should().Be($"books/{shardedId[..2]}/{shardedId}/cover.jpg");
        (await verifyContext.Books.SingleAsync()).Id.Should().Be(bookId);
    }

    [Fact]
    public async Task Migration_replaces_an_abandoned_temporary_backup_from_an_interrupted_start()
    {
        using var library = new TemporaryLibrary();
        var (factory, bookId) = await CreateLegacyBookAsync(library.DirectoryPath, includeCover: false);
        var layout = new ManagedLibraryStorageLayout(library.DirectoryPath);
        await File.WriteAllBytesAsync(Path.Combine(layout.GetLegacyBookDirectory(bookId), "book.epub"), [1, 2, 3]);
        var backupDirectory = Directory.CreateDirectory(Path.Combine(library.DirectoryPath, "backups"));
        var temporaryBackupPath = Path.Combine(
            backupDirectory.FullName,
            $".{LibraryStorageMigrator.DatabaseBackupFileName}.tmp");
        await File.WriteAllTextAsync(temporaryBackupPath, "interrupted");

        var result = await new LibraryStorageMigrator(factory).MigrateAsync(
            new LibraryDescriptor("Test", library.DirectoryPath, DateTimeOffset.UtcNow),
            progress: null,
            default);

        result.Status.Should().Be(LibraryStorageMigrationStatus.Completed);
        File.Exists(temporaryBackupPath).Should().BeFalse();
        File.Exists(Path.Combine(
            backupDirectory.FullName,
            LibraryStorageMigrator.DatabaseBackupFileName)).Should().BeTrue();
    }

    [Fact]
    public async Task Migration_detects_a_missing_already_sharded_storage_directory()
    {
        using var library = new TemporaryLibrary();
        var (factory, bookId) = await CreateLegacyBookAsync(
            library.DirectoryPath,
            includeCover: false,
            createLegacyDirectory: false);
        await using (var context = factory.Create(library.DirectoryPath))
        {
            var id = bookId.ToString("N");
            (await context.BookFiles.SingleAsync()).RelativePath = $"books/{id[..2]}/{id}/book.epub";
            await context.SaveChangesAsync();
        }

        var action = () => new LibraryStorageMigrator(factory).MigrateAsync(
            new LibraryDescriptor("Test", library.DirectoryPath, DateTimeOffset.UtcNow),
            progress: null,
            default);

        (await action.Should().ThrowAsync<LibraryStorageMigrationException>())
            .Which.StorageId.Should().Be(bookId);
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

using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Libraries;
using EbookManager.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Infrastructure.Files;

public sealed class LibraryStorageMigrator(LibraryDbContextFactory contextFactory) : ILibraryStorageMigrator
{
    public const string DatabaseBackupFileName = "library-before-sharded-storage-v2.db";

    public async Task<LibraryStorageMigrationResult> MigrateAsync(
        LibraryDescriptor library,
        IProgress<LibraryStorageMigrationProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(library);
        cancellationToken.ThrowIfCancellationRequested();

        var layout = new ManagedLibraryStorageLayout(library.DirectoryPath);
        Directory.CreateDirectory(layout.BooksDirectory);
        layout.EnsureNoReparsePoints(layout.BooksDirectory);

        var snapshots = await LoadSnapshotsAsync(library.DirectoryPath, layout, cancellationToken);
        var candidates = snapshots
            .Where(snapshot => RequiresMigration(snapshot, layout))
            .OrderBy(snapshot => snapshot.StorageId)
            .ToArray();
        if (candidates.Length == 0)
        {
            return new(LibraryStorageMigrationStatus.NotRequired, 0, 0);
        }

        progress?.Report(new(0, 0, null));
        try
        {
            await LibraryDatabaseBackup.EnsureAsync(
                library.DirectoryPath,
                layout,
                DatabaseBackupFileName,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (LibraryStorageMigrationException)
        {
            throw;
        }
        catch (Exception exception) when (IsExpectedStorageException(exception))
        {
            throw CreateException(
                storageId: null,
                layout.GetAbsolutePath(Path.Combine("backups", DatabaseBackupFileName)),
                "The database backup could not be created or verified.",
                exception);
        }

        progress?.Report(new(candidates.Length, 0, null));
        var migratedCount = 0;
        foreach (var snapshot in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await MigrateDirectoryAsync(library.DirectoryPath, snapshot, layout, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (LibraryStorageMigrationException)
            {
                throw;
            }
            catch (Exception exception) when (IsExpectedStorageException(exception))
            {
                throw CreateException(
                    snapshot.StorageId,
                    layout.GetBookDirectory(snapshot.StorageId),
                    "The book storage could not be migrated.",
                    exception);
            }

            migratedCount++;
            progress?.Report(new(candidates.Length, migratedCount, snapshot.StorageId));
        }

        return new(LibraryStorageMigrationStatus.Completed, candidates.Length, migratedCount);
    }

    private async Task MigrateDirectoryAsync(
        string libraryPath,
        StorageDirectorySnapshot snapshot,
        ManagedLibraryStorageLayout layout,
        CancellationToken cancellationToken)
    {
        var legacyDirectory = layout.GetLegacyBookDirectory(snapshot.StorageId);
        var targetDirectory = layout.GetBookDirectory(snapshot.StorageId);
        var legacyExists = Directory.Exists(legacyDirectory);
        var targetExists = Directory.Exists(targetDirectory);
        var paths = snapshot.RelativePaths;
        var legacyPrefix = layout.GetLegacyRelativeBookDirectory(snapshot.StorageId) + "/";
        var targetPrefix = layout.GetRelativeBookDirectory(snapshot.StorageId) + "/";
        var legacyPathCount = paths.Count(path => StartsWithPath(path, legacyPrefix));
        var targetPathCount = paths.Count(path => StartsWithPath(path, targetPrefix));
        var unknownPathCount = paths.Count - legacyPathCount - targetPathCount;

        if (legacyExists && targetExists)
        {
            throw CreateException(snapshot.StorageId, targetDirectory, "Both storage locations exist.");
        }

        if (unknownPathCount > 0 || (legacyPathCount > 0 && targetPathCount > 0))
        {
            throw CreateException(snapshot.StorageId, legacyDirectory, "The stored paths do not have one consistent layout.");
        }

        if (legacyExists)
        {
            if (targetPathCount > 0)
            {
                throw CreateException(snapshot.StorageId, legacyDirectory, "The database already points at a missing target location.");
            }

            layout.EnsureNoReparsePoints(legacyDirectory);
            var targetParent = Path.GetDirectoryName(targetDirectory)
                ?? throw CreateException(snapshot.StorageId, targetDirectory, "The target has no parent directory.");
            layout.EnsureNoReparsePoints(targetParent);
            Directory.CreateDirectory(targetParent);
            layout.EnsureNoReparsePoints(targetParent);

            try
            {
                Directory.Move(legacyDirectory, targetDirectory);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw CreateException(snapshot.StorageId, legacyDirectory, "The book directory could not be moved.", exception);
            }

            targetExists = true;
        }

        if (!targetExists)
        {
            throw CreateException(snapshot.StorageId, legacyDirectory, "The stored book directory is missing.");
        }

        layout.EnsureNoReparsePoints(targetDirectory);
        EnsureExpectedFilesExist(
            snapshot.StorageId,
            paths,
            legacyPrefix,
            targetPrefix,
            layout);
        if (legacyPathCount > 0)
        {
            await UpdateRelativePathsAsync(
                libraryPath,
                snapshot.StorageId,
                legacyPrefix,
                targetPrefix,
                cancellationToken);
        }
    }

    private async Task UpdateRelativePathsAsync(
        string libraryPath,
        Guid storageId,
        string legacyPrefix,
        string targetPrefix,
        CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var books = await context.Books
            .Where(entity => entity.CoverRelativePath != null &&
                entity.CoverRelativePath.StartsWith(legacyPrefix))
            .ToListAsync(cancellationToken);
        var files = await context.BookFiles
            .Where(entity => entity.RelativePath.StartsWith(legacyPrefix))
            .ToListAsync(cancellationToken);

        foreach (var book in books)
        {
            book.CoverRelativePath = RewritePath(
                book.CoverRelativePath!,
                legacyPrefix,
                targetPrefix,
                storageId);
        }

        foreach (var file in files)
        {
            file.RelativePath = RewritePath(file.RelativePath, legacyPrefix, targetPrefix, storageId);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static string RewritePath(
        string relativePath,
        string legacyPrefix,
        string targetPrefix,
        Guid storageId)
    {
        if (!StartsWithPath(relativePath, legacyPrefix))
        {
            throw CreateException(storageId, relativePath, "A database path changed during migration.");
        }

        return targetPrefix + relativePath[legacyPrefix.Length..];
    }

    private async Task<IReadOnlyList<StorageDirectorySnapshot>> LoadSnapshotsAsync(
        string libraryPath,
        ManagedLibraryStorageLayout layout,
        CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        var books = await context.Books
            .AsNoTracking()
            .Select(entity => new { entity.Id, entity.CoverRelativePath })
            .ToListAsync(cancellationToken);
        var files = await context.BookFiles
            .AsNoTracking()
            .Select(entity => entity.RelativePath)
            .ToListAsync(cancellationToken);
        var pathsByStorageId = new Dictionary<Guid, List<string>>();
        foreach (var relativePath in files.Concat(
                     books.Select(book => book.CoverRelativePath)
                         .Where(path => !string.IsNullOrWhiteSpace(path))
                         .Cast<string>()))
        {
            if (!TryGetStorageId(relativePath, out var storageId))
            {
                throw CreateException(
                    storageId: null,
                    relativePath,
                    "A managed database path has an unsupported storage layout.");
            }

            if (!pathsByStorageId.TryGetValue(storageId, out var paths))
            {
                paths = [];
                pathsByStorageId.Add(storageId, paths);
            }

            paths.Add(relativePath);
        }

        foreach (var book in books)
        {
            if ((Directory.Exists(layout.GetLegacyBookDirectory(book.Id)) ||
                 Directory.Exists(layout.GetBookDirectory(book.Id))) &&
                !pathsByStorageId.ContainsKey(book.Id))
            {
                pathsByStorageId.Add(book.Id, []);
            }
        }

        return pathsByStorageId
            .Select(pair => new StorageDirectorySnapshot(pair.Key, pair.Value.AsReadOnly()))
            .ToArray();
    }

    private static bool RequiresMigration(
        StorageDirectorySnapshot snapshot,
        ManagedLibraryStorageLayout layout)
    {
        if (Directory.Exists(layout.GetLegacyBookDirectory(snapshot.StorageId)))
        {
            return true;
        }

        var legacyPrefix = layout.GetLegacyRelativeBookDirectory(snapshot.StorageId) + "/";
        return snapshot.RelativePaths.Any(path => StartsWithPath(path, legacyPrefix));
    }

    private static bool TryGetStorageId(string relativePath, out Guid storageId)
    {
        storageId = Guid.Empty;
        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 3 &&
            string.Equals(segments[0], "books", StringComparison.OrdinalIgnoreCase))
        {
            return Guid.TryParseExact(segments[1], "N", out storageId) && storageId != Guid.Empty;
        }

        if (segments.Length == 4 &&
            string.Equals(segments[0], "books", StringComparison.OrdinalIgnoreCase) &&
            Guid.TryParseExact(segments[2], "N", out storageId) &&
            storageId != Guid.Empty)
        {
            return string.Equals(
                segments[1],
                storageId.ToString("N")[..2],
                StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static void EnsureExpectedFilesExist(
        Guid storageId,
        IReadOnlyList<string> paths,
        string legacyPrefix,
        string targetPrefix,
        ManagedLibraryStorageLayout layout)
    {
        foreach (var path in paths)
        {
            var suffix = StartsWithPath(path, legacyPrefix)
                ? path[legacyPrefix.Length..]
                : path[targetPrefix.Length..];
            var expectedPath = layout.GetAbsolutePath(targetPrefix + suffix);
            if (!File.Exists(expectedPath))
            {
                throw CreateException(storageId, expectedPath, "An expected managed book file is missing.");
            }
        }
    }

    private static bool StartsWithPath(string path, string prefix) =>
        path.StartsWith(
            prefix,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static bool IsExpectedStorageException(Exception exception) =>
        exception is IOException or
            UnauthorizedAccessException or
            InvalidOperationException or
            SqliteException or
            DbUpdateException;

    private static LibraryStorageMigrationException CreateException(
        Guid? storageId,
        string path,
        string message,
        Exception? innerException = null) =>
        new(storageId, path, message, innerException);

    private sealed record StorageDirectorySnapshot(
        Guid StorageId,
        IReadOnlyList<string> RelativePaths);
}

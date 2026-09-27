using EbookManager.Domain.Libraries;
using EbookManager.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Infrastructure.Files;

public sealed class LibraryStorageMigrator(LibraryDbContextFactory contextFactory)
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

        var snapshots = await LoadSnapshotsAsync(library.DirectoryPath, cancellationToken);
        var candidates = snapshots
            .Where(snapshot => RequiresMigration(snapshot, layout))
            .OrderBy(snapshot => snapshot.BookId)
            .ToArray();
        if (candidates.Length == 0)
        {
            return new(LibraryStorageMigrationStatus.NotRequired, 0, 0);
        }

        await EnsureDatabaseBackupAsync(library.DirectoryPath, layout, cancellationToken);

        var migratedCount = 0;
        foreach (var snapshot in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await MigrateBookAsync(library.DirectoryPath, snapshot, layout, cancellationToken);
            migratedCount++;
            progress?.Report(new(candidates.Length, migratedCount, snapshot.BookId));
        }

        return new(LibraryStorageMigrationStatus.Completed, candidates.Length, migratedCount);
    }

    private async Task MigrateBookAsync(
        string libraryPath,
        BookStorageSnapshot snapshot,
        ManagedLibraryStorageLayout layout,
        CancellationToken cancellationToken)
    {
        var legacyDirectory = layout.GetLegacyBookDirectory(snapshot.BookId);
        var targetDirectory = layout.GetBookDirectory(snapshot.BookId);
        var legacyExists = Directory.Exists(legacyDirectory);
        var targetExists = Directory.Exists(targetDirectory);
        var paths = snapshot.FileRelativePaths
            .Prepend(snapshot.CoverRelativePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>()
            .ToArray();
        var legacyPrefix = layout.GetLegacyRelativeBookDirectory(snapshot.BookId) + "/";
        var targetPrefix = layout.GetRelativeBookDirectory(snapshot.BookId) + "/";
        var legacyPathCount = paths.Count(path => StartsWithPath(path, legacyPrefix));
        var targetPathCount = paths.Count(path => StartsWithPath(path, targetPrefix));
        var unknownPathCount = paths.Length - legacyPathCount - targetPathCount;

        if (legacyExists && targetExists)
        {
            throw CreateException(snapshot.BookId, targetDirectory, "Both storage locations exist.");
        }

        if (unknownPathCount > 0 || (legacyPathCount > 0 && targetPathCount > 0))
        {
            throw CreateException(snapshot.BookId, legacyDirectory, "The stored paths do not have one consistent layout.");
        }

        if (legacyExists)
        {
            if (targetPathCount > 0)
            {
                throw CreateException(snapshot.BookId, legacyDirectory, "The database already points at a missing target location.");
            }

            layout.EnsureNoReparsePoints(legacyDirectory);
            var targetParent = Path.GetDirectoryName(targetDirectory)
                ?? throw CreateException(snapshot.BookId, targetDirectory, "The target has no parent directory.");
            layout.EnsureNoReparsePoints(targetParent);
            Directory.CreateDirectory(targetParent);
            layout.EnsureNoReparsePoints(targetParent);

            try
            {
                Directory.Move(legacyDirectory, targetDirectory);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw CreateException(snapshot.BookId, legacyDirectory, "The book directory could not be moved.", exception);
            }

            targetExists = true;
        }

        if (!targetExists)
        {
            throw CreateException(snapshot.BookId, legacyDirectory, "The stored book directory is missing.");
        }

        layout.EnsureNoReparsePoints(targetDirectory);
        EnsureExpectedFilesExist(
            snapshot.BookId,
            paths,
            legacyPrefix,
            targetPrefix,
            layout);
        if (legacyPathCount > 0)
        {
            await UpdateRelativePathsAsync(
                libraryPath,
                snapshot.BookId,
                legacyPrefix,
                targetPrefix,
                cancellationToken);
        }
    }

    private async Task UpdateRelativePathsAsync(
        string libraryPath,
        Guid bookId,
        string legacyPrefix,
        string targetPrefix,
        CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var book = await context.Books
            .SingleOrDefaultAsync(entity => entity.Id == bookId, cancellationToken)
            ?? throw CreateException(bookId, legacyPrefix, "The book no longer exists in the database.");
        var files = await context.BookFiles
            .Where(entity => entity.BookId == bookId)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(book.CoverRelativePath))
        {
            book.CoverRelativePath = RewritePath(book.CoverRelativePath, legacyPrefix, targetPrefix, bookId);
        }

        foreach (var file in files)
        {
            file.RelativePath = RewritePath(file.RelativePath, legacyPrefix, targetPrefix, bookId);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static string RewritePath(
        string relativePath,
        string legacyPrefix,
        string targetPrefix,
        Guid bookId)
    {
        if (!StartsWithPath(relativePath, legacyPrefix))
        {
            throw CreateException(bookId, relativePath, "A database path changed during migration.");
        }

        return targetPrefix + relativePath[legacyPrefix.Length..];
    }

    private async Task<IReadOnlyList<BookStorageSnapshot>> LoadSnapshotsAsync(
        string libraryPath,
        CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        var books = await context.Books
            .AsNoTracking()
            .Select(entity => new { entity.Id, entity.CoverRelativePath })
            .ToListAsync(cancellationToken);
        var files = await context.BookFiles
            .AsNoTracking()
            .Select(entity => new { entity.BookId, entity.RelativePath })
            .ToListAsync(cancellationToken);
        var filesByBookId = files
            .GroupBy(file => file.BookId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(file => file.RelativePath).ToArray());

        return books
            .Select(book => new BookStorageSnapshot(
                book.Id,
                book.CoverRelativePath,
                filesByBookId.GetValueOrDefault(book.Id) ?? []))
            .ToArray();
    }

    private static bool RequiresMigration(
        BookStorageSnapshot snapshot,
        ManagedLibraryStorageLayout layout)
    {
        if (Directory.Exists(layout.GetLegacyBookDirectory(snapshot.BookId)))
        {
            return true;
        }

        var legacyPrefix = layout.GetLegacyRelativeBookDirectory(snapshot.BookId) + "/";
        return (!string.IsNullOrWhiteSpace(snapshot.CoverRelativePath) &&
                StartsWithPath(snapshot.CoverRelativePath, legacyPrefix)) ||
            snapshot.FileRelativePaths.Any(path => StartsWithPath(path, legacyPrefix));
    }

    private static async Task EnsureDatabaseBackupAsync(
        string libraryPath,
        ManagedLibraryStorageLayout layout,
        CancellationToken cancellationToken)
    {
        var backupDirectory = layout.GetAbsolutePath("backups");
        layout.EnsureNoReparsePoints(backupDirectory);
        var backupPath = layout.GetAbsolutePath(Path.Combine(backupDirectory, DatabaseBackupFileName));
        if (File.Exists(backupPath))
        {
            layout.EnsureNoReparsePoints(backupPath);
            if (!await HasSqliteHeaderAsync(backupPath, cancellationToken))
            {
                throw new InvalidOperationException("The existing storage migration backup is not a valid SQLite database.");
            }

            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(backupDirectory);
        layout.EnsureNoReparsePoints(backupDirectory);
        var temporaryPath = layout.GetAbsolutePath(Path.Combine(backupDirectory, $".{Guid.NewGuid():N}.db.tmp"));
        try
        {
            var sourceConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(libraryPath, "library.db"),
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            }.ToString();
            var destinationConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = temporaryPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            }.ToString();
            await using var source = new SqliteConnection(sourceConnectionString);
            await using var destination = new SqliteConnection(destinationConnectionString);
            await source.OpenAsync(cancellationToken);
            await destination.OpenAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            source.BackupDatabase(destination);
            await destination.CloseAsync();
            await source.CloseAsync();
            cancellationToken.ThrowIfCancellationRequested();
            if (!await HasSqliteHeaderAsync(temporaryPath, cancellationToken))
            {
                throw new InvalidOperationException("The storage migration backup could not be verified.");
            }

            File.Move(temporaryPath, backupPath);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void EnsureExpectedFilesExist(
        Guid bookId,
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
                throw CreateException(bookId, expectedPath, "An expected managed book file is missing.");
            }
        }
    }

    private static async Task<bool> HasSqliteHeaderAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var expectedHeader = "SQLite format 3\0"u8.ToArray();
        var actualHeader = new byte[expectedHeader.Length];
        await using var stream = new FileStream(
            path,
            new FileStreamOptions
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Share = FileShare.Read,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan
            });
        var bytesRead = await stream.ReadAsync(actualHeader, cancellationToken);
        return bytesRead == expectedHeader.Length && actualHeader.AsSpan().SequenceEqual(expectedHeader);
    }

    private static bool StartsWithPath(string path, string prefix) =>
        path.StartsWith(
            prefix,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static LibraryStorageMigrationException CreateException(
        Guid bookId,
        string path,
        string message,
        Exception? innerException = null) =>
        new(bookId, path, message, innerException);

    private sealed record BookStorageSnapshot(
        Guid BookId,
        string? CoverRelativePath,
        IReadOnlyList<string> FileRelativePaths);
}

public enum LibraryStorageMigrationStatus
{
    NotRequired,
    Completed
}

public sealed record LibraryStorageMigrationResult(
    LibraryStorageMigrationStatus Status,
    int TotalCount,
    int MigratedCount);

public sealed record LibraryStorageMigrationProgress(
    int TotalCount,
    int ProcessedCount,
    Guid BookId);

public sealed class LibraryStorageMigrationException(
    Guid bookId,
    string path,
    string message,
    Exception? innerException = null) : IOException(message, innerException)
{
    public Guid BookId { get; } = bookId;
    public string Path { get; } = path;
}

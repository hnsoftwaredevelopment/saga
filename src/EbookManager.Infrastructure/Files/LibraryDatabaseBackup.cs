using Microsoft.Data.Sqlite;

namespace EbookManager.Infrastructure.Files;

internal static class LibraryDatabaseBackup
{
    public static async Task EnsureAsync(
        string libraryPath,
        ManagedLibraryStorageLayout layout,
        string backupFileName,
        CancellationToken cancellationToken)
    {
        var backupDirectory = layout.GetAbsolutePath("backups");
        layout.EnsureNoReparsePoints(backupDirectory);
        var backupPath = layout.GetAbsolutePath(Path.Combine(backupDirectory, backupFileName));
        if (File.Exists(backupPath))
        {
            layout.EnsureNoReparsePoints(backupPath);
            if (!await HasSqliteHeaderAsync(backupPath, cancellationToken))
            {
                throw new InvalidOperationException(
                    "The existing storage migration backup is not a valid SQLite database.");
            }

            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(backupDirectory);
        layout.EnsureNoReparsePoints(backupDirectory);
        var temporaryPath = layout.GetAbsolutePath(
            Path.Combine(backupDirectory, $".{Guid.NewGuid():N}.db.tmp"));
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
}

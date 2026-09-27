namespace EbookManager.Domain.Libraries;

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
    Guid? StorageId);

public sealed class LibraryStorageMigrationException(
    Guid? storageId,
    string path,
    string message,
    Exception? innerException = null) : IOException(message, innerException)
{
    public Guid? StorageId { get; } = storageId;
    public string Path { get; } = path;
}

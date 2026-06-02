namespace EbookManager.Domain.Abstractions;

public interface ILibraryFileStore
{
    string GetAbsolutePath(string relativePath);

    Task<(string RelativeBookPath, string? RelativeCoverPath)> CopyIntoLibraryAsync(
        Guid bookId,
        string sourcePath,
        byte[]? coverBytes,
        CancellationToken cancellationToken);
    Task DeleteBookDirectoryAsync(Guid bookId, CancellationToken cancellationToken);
}

namespace EbookManager.Domain.Abstractions;

public interface IHashingLibraryFileStore : ILibraryFileStore
{
    Task<(string RelativeBookPath, string? RelativeCoverPath, string Sha256)> CopyIntoLibraryWithHashAsync(
        Guid bookId,
        string sourcePath,
        byte[]? coverBytes,
        CancellationToken cancellationToken);
}

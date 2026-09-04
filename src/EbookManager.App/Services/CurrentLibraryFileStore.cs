using EbookManager.Domain.Abstractions;
using EbookManager.Infrastructure.Files;
using EbookManager.Libraries;

namespace EbookManager.App.Services;

public sealed class CurrentLibraryFileStore(CurrentLibrary currentLibrary) : IHashingLibraryFileStore, IBookCoverStore
{
    public Task<(string RelativeBookPath, string? RelativeCoverPath)> CopyIntoLibraryAsync(
        Guid bookId,
        string sourcePath,
        byte[]? coverBytes,
        CancellationToken cancellationToken) =>
        CreateStore().CopyIntoLibraryAsync(bookId, sourcePath, coverBytes, cancellationToken);

    public Task DeleteBookDirectoryAsync(Guid bookId, CancellationToken cancellationToken) =>
        CreateStore().DeleteBookDirectoryAsync(bookId, cancellationToken);

    public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken) =>
        CreateStore().DeleteFileAsync(relativePath, cancellationToken);

    public Task<(string RelativeBookPath, string? RelativeCoverPath, string Sha256)> CopyIntoLibraryWithHashAsync(
        Guid bookId,
        string sourcePath,
        byte[]? coverBytes,
        CancellationToken cancellationToken) =>
        CreateStore().CopyIntoLibraryWithHashAsync(bookId, sourcePath, coverBytes, cancellationToken);

    public string GetAbsolutePath(string relativePath) => CreateStore().GetAbsolutePath(relativePath);

    public Task<string> SaveAsync(
        Guid bookId,
        byte[] coverBytes,
        CancellationToken cancellationToken) =>
        CreateCoverStore().SaveAsync(bookId, coverBytes, cancellationToken);

    public Task DeleteAsync(Guid bookId, CancellationToken cancellationToken) =>
        CreateCoverStore().DeleteAsync(bookId, cancellationToken);

    private ManagedLibraryFileStore CreateStore()
    {
        var library = currentLibrary.Current ?? throw new InvalidOperationException("No active library is loaded.");
        return new ManagedLibraryFileStore(library.DirectoryPath);
    }

    private ManagedLibraryCoverStore CreateCoverStore()
    {
        var library = currentLibrary.Current ?? throw new InvalidOperationException("No active library is loaded.");
        return new ManagedLibraryCoverStore(library.DirectoryPath);
    }
}

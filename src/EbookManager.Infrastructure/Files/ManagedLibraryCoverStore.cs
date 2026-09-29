using EbookManager.Domain.Abstractions;

namespace EbookManager.Infrastructure.Files;

public sealed class ManagedLibraryCoverStore(string libraryRootPath) : IBookCoverStore
{
    private const int MaximumCoverBytes = 10 * 1024 * 1024;
    private readonly ManagedLibraryStorageLayout layout = new(libraryRootPath);

    public async Task<string> SaveAsync(
        Guid bookId,
        byte[] coverBytes,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(bookId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(coverBytes);
        if (coverBytes.Length is 0 or > MaximumCoverBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(coverBytes));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var bookDirectory = layout.ResolveExistingOrNewBookDirectory(bookId);
        layout.EnsureNoReparsePoints(bookDirectory);
        Directory.CreateDirectory(bookDirectory);
        layout.EnsureNoReparsePoints(bookDirectory);
        var coverPath = layout.GetAbsolutePath(Path.Combine(bookDirectory, "cover.jpg"));
        layout.EnsureNoReparsePoints(coverPath);
        var temporaryPath = layout.GetAbsolutePath(Path.Combine(bookDirectory, $".{Guid.NewGuid():N}.cover.tmp"));

        try
        {
            layout.EnsureNoReparsePoints(temporaryPath);
            layout.EnsureNoReparsePoints(coverPath);
            await File.WriteAllBytesAsync(temporaryPath, coverBytes, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            layout.EnsureNoReparsePoints(temporaryPath);
            layout.EnsureNoReparsePoints(coverPath);
            File.Move(temporaryPath, coverPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                layout.EnsureNoReparsePoints(temporaryPath);
                File.Delete(temporaryPath);
            }
        }

        return layout.ToRelativePath(coverPath);
    }

    public Task DeleteAsync(Guid bookId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(bookId, Guid.Empty);
        cancellationToken.ThrowIfCancellationRequested();
        var bookDirectory = layout.ResolveExistingOrNewBookDirectory(bookId);
        var coverPath = layout.GetAbsolutePath(Path.Combine(bookDirectory, "cover.jpg"));
        layout.EnsureNoReparsePoints(coverPath);
        if (File.Exists(coverPath))
        {
            File.Delete(coverPath);
        }

        return Task.CompletedTask;
    }

}

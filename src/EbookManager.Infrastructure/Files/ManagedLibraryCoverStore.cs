using EbookManager.Domain.Abstractions;

namespace EbookManager.Infrastructure.Files;

public sealed class ManagedLibraryCoverStore(string libraryRootPath) : IBookCoverStore
{
    private const int MaximumCoverBytes = 10 * 1024 * 1024;
    private readonly string libraryRoot = Canonicalize(libraryRootPath);

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
        var bookDirectory = EnsureContained(Path.Combine(libraryRoot, "books", bookId.ToString("N")));
        Directory.CreateDirectory(bookDirectory);
        var coverPath = EnsureContained(Path.Combine(bookDirectory, "cover.jpg"));
        var temporaryPath = EnsureContained(Path.Combine(bookDirectory, $".{Guid.NewGuid():N}.cover.tmp"));

        try
        {
            await File.WriteAllBytesAsync(temporaryPath, coverBytes, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, coverPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        return ToRelativePath(coverPath);
    }

    public Task DeleteAsync(Guid bookId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(bookId, Guid.Empty);
        cancellationToken.ThrowIfCancellationRequested();
        var coverPath = EnsureContained(Path.Combine(libraryRoot, "books", bookId.ToString("N"), "cover.jpg"));
        if (File.Exists(coverPath))
        {
            File.Delete(coverPath);
        }

        return Task.CompletedTask;
    }

    private static string Canonicalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("The library root path must not be blank.", nameof(path));
        }

        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    private string EnsureContained(string path)
    {
        var canonicalPath = Path.GetFullPath(path);
        var rootWithSeparator = libraryRoot + Path.DirectorySeparatorChar;
        if (!canonicalPath.StartsWith(
                rootWithSeparator,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The cover path escapes the active library.");
        }

        return canonicalPath;
    }

    private string ToRelativePath(string absolutePath) =>
        Path.GetRelativePath(libraryRoot, absolutePath).Replace(Path.DirectorySeparatorChar, '/');
}

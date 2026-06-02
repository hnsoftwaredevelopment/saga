using EbookManager.Domain.Abstractions;

namespace EbookManager.Infrastructure.Files;

public sealed class ManagedLibraryFileStore(string libraryRootPath) : ILibraryFileStore
{
    private readonly string libraryRoot = Canonicalize(libraryRootPath);

    public async Task<(string RelativeBookPath, string? RelativeCoverPath)> CopyIntoLibraryAsync(
        Guid bookId,
        string sourcePath,
        byte[]? coverBytes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var bookDirectory = GetBookDirectory(bookId);
        var managedSourceName = Path.GetFileName(sourcePath);
        if (string.IsNullOrWhiteSpace(managedSourceName))
        {
            throw new ArgumentException("The source path must include a file name.", nameof(sourcePath));
        }

        await using var source = new FileStream(
            Path.GetFullPath(sourcePath),
            new FileStreamOptions
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Share = FileShare.Read,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan
            });

        Directory.CreateDirectory(bookDirectory);

        var absoluteBookPath = EnsureContained(Path.Combine(bookDirectory, managedSourceName));
        var temporaryBookPath = $"{absoluteBookPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            await using (var destination = new FileStream(
                temporaryBookPath,
                new FileStreamOptions
                {
                    Access = FileAccess.Write,
                    Mode = FileMode.CreateNew,
                    Share = FileShare.None,
                    Options = FileOptions.Asynchronous | FileOptions.SequentialScan
                }))
            {
                await source.CopyToAsync(destination, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryBookPath, absoluteBookPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryBookPath))
            {
                File.Delete(temporaryBookPath);
            }
        }

        string? relativeCoverPath = null;
        if (coverBytes is { Length: > 0 })
        {
            cancellationToken.ThrowIfCancellationRequested();

            var absoluteCoverPath = EnsureContained(Path.Combine(bookDirectory, "cover.jpg"));
            await File.WriteAllBytesAsync(absoluteCoverPath, coverBytes, cancellationToken);
            relativeCoverPath = ToRelativePath(absoluteCoverPath);
        }

        return (ToRelativePath(absoluteBookPath), relativeCoverPath);
    }

    public Task DeleteBookDirectoryAsync(Guid bookId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var bookDirectory = EnsureContained(GetBookDirectory(bookId));
        if (Directory.Exists(bookDirectory))
        {
            Directory.Delete(bookDirectory, recursive: true);
        }

        return Task.CompletedTask;
    }

    private string GetBookDirectory(Guid bookId) => Path.Combine(libraryRoot, "books", bookId.ToString("N"));

    private string EnsureContained(string path)
    {
        var fullPath = Canonicalize(path);
        var rootWithSeparator = libraryRoot.EndsWith(Path.DirectorySeparatorChar)
            ? libraryRoot
            : $"{libraryRoot}{Path.DirectorySeparatorChar}";

        if (!fullPath.Equals(libraryRoot, StringComparison.OrdinalIgnoreCase) &&
            !fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Path '{path}' escapes the managed library root.");
        }

        return fullPath;
    }

    private static string Canonicalize(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    private string ToRelativePath(string absolutePath) =>
        Path.GetRelativePath(libraryRoot, absolutePath).Replace(Path.DirectorySeparatorChar, '/');
}

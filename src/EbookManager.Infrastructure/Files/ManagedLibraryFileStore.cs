using System.Buffers;
using System.Security.Cryptography;
using EbookManager.Domain.Abstractions;

namespace EbookManager.Infrastructure.Files;

public sealed class ManagedLibraryFileStore(string libraryRootPath) : IHashingLibraryFileStore
{
    private readonly string libraryRoot = Canonicalize(libraryRootPath);

    public string GetAbsolutePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("The relative path must not be blank.", nameof(relativePath));
        }

        return EnsureContained(Path.Combine(libraryRoot, relativePath));
    }

    public async Task<(string RelativeBookPath, string? RelativeCoverPath)> CopyIntoLibraryAsync(
        Guid bookId,
        string sourcePath,
        byte[]? coverBytes,
        CancellationToken cancellationToken)
    {
        var copy = await CopyIntoLibraryCoreAsync(
            bookId,
            sourcePath,
            coverBytes,
            computeSha256: false,
            cancellationToken);
        return (copy.RelativeBookPath, copy.RelativeCoverPath);
    }

    public async Task<(string RelativeBookPath, string? RelativeCoverPath, string Sha256)> CopyIntoLibraryWithHashAsync(
        Guid bookId,
        string sourcePath,
        byte[]? coverBytes,
        CancellationToken cancellationToken)
    {
        var copy = await CopyIntoLibraryCoreAsync(
            bookId,
            sourcePath,
            coverBytes,
            computeSha256: true,
            cancellationToken);
        return (copy.RelativeBookPath, copy.RelativeCoverPath, copy.Sha256!);
    }

    private async Task<(string RelativeBookPath, string? RelativeCoverPath, string? Sha256)> CopyIntoLibraryCoreAsync(
        Guid bookId,
        string sourcePath,
        byte[]? coverBytes,
        bool computeSha256,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var booksDirectory = EnsureContained(Path.Combine(libraryRoot, "books"));
        Directory.CreateDirectory(booksDirectory);

        var bookDirectory = GetBookDirectory(bookId);
        var stagingDirectory = EnsureContained(Path.Combine(
            booksDirectory,
            $".{bookId:N}.{Guid.NewGuid():N}.staging"));
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
                Share = FileShare.ReadWrite | FileShare.Delete,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan
            });

        Directory.CreateDirectory(stagingDirectory);
        var absoluteBookPath = EnsureContained(Path.Combine(bookDirectory, managedSourceName));
        var stagedBookPath = EnsureContained(Path.Combine(stagingDirectory, managedSourceName));
        string? hash = null;

        try
        {
            await using (var destination = new FileStream(
                stagedBookPath,
                new FileStreamOptions
                {
                    Access = FileAccess.Write,
                    Mode = FileMode.Create,
                    Share = FileShare.None,
                    Options = FileOptions.Asynchronous | FileOptions.SequentialScan
                }))
            {
                var sha256 = computeSha256
                    ? await CopyToAsyncAndHashAsync(source, destination, cancellationToken)
                    : null;
                if (!computeSha256)
                {
                    await source.CopyToAsync(destination, cancellationToken);
                }

                hash = sha256;
            }

            string? stagedCoverPath = null;
            if (coverBytes is { Length: > 0 })
            {
                cancellationToken.ThrowIfCancellationRequested();
                stagedCoverPath = EnsureContained(Path.Combine(stagingDirectory, "cover.jpg"));
                await File.WriteAllBytesAsync(stagedCoverPath, coverBytes, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            var targetExisted = Directory.Exists(bookDirectory);
            if (!targetExisted)
            {
                Directory.Move(stagingDirectory, bookDirectory);
            }
            else
            {
                File.Move(stagedBookPath, absoluteBookPath, overwrite: true);

                if (stagedCoverPath is not null)
                {
                    File.Move(
                        stagedCoverPath,
                        EnsureContained(Path.Combine(bookDirectory, "cover.jpg")),
                        overwrite: true);
                }
            }

            return (
                ToRelativePath(absoluteBookPath),
                stagedCoverPath is null ? null : ToRelativePath(Path.Combine(bookDirectory, "cover.jpg")),
                hash);
        }
        finally
        {
            if (Directory.Exists(stagingDirectory))
            {
                TryDeleteDirectory(stagingDirectory);
            }
        }
    }

    private static async Task<string> CopyToAsyncAndHashAsync(
        Stream source,
        Stream destination,
        CancellationToken cancellationToken)
    {
        using var incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024);
        try
        {
            while (true)
            {
                var bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                incrementalHash.AppendData(buffer.AsSpan(0, bytesRead));
                await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }

            return Convert.ToHexString(incrementalHash.GetHashAndReset());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
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
        var pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var rootWithSeparator = libraryRoot.EndsWith(Path.DirectorySeparatorChar)
            ? libraryRoot
            : $"{libraryRoot}{Path.DirectorySeparatorChar}";

        if (!fullPath.Equals(libraryRoot, pathComparison) &&
            !fullPath.StartsWith(rootWithSeparator, pathComparison))
        {
            throw new InvalidOperationException($"Path '{path}' escapes the managed library root.");
        }

        return fullPath;
    }

    private static string Canonicalize(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    private string ToRelativePath(string absolutePath) =>
        Path.GetRelativePath(libraryRoot, absolutePath).Replace(Path.DirectorySeparatorChar, '/');

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch
        {
        }
    }
}

using System.Buffers;
using System.Security.Cryptography;
using EbookManager.Domain.Abstractions;

namespace EbookManager.Infrastructure.Files;

public sealed class ManagedLibraryFileStore(string libraryRootPath) : IHashingLibraryFileStore
{
    private readonly ManagedLibraryStorageLayout layout = new(libraryRootPath);

    public string GetAbsolutePath(string relativePath)
    {
        return layout.GetAbsolutePath(relativePath);
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
        var bookDirectory = layout.ResolveExistingOrNewBookDirectory(bookId);
        var parentDirectory = Path.GetDirectoryName(bookDirectory)
            ?? throw new InvalidOperationException("The managed book directory has no parent directory.");
        layout.EnsureNoReparsePoints(parentDirectory);
        Directory.CreateDirectory(parentDirectory);
        layout.EnsureNoReparsePoints(parentDirectory);
        var stagingDirectory = layout.GetStagingDirectory(bookId, Guid.NewGuid());
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
        layout.EnsureNoReparsePoints(stagingDirectory);
        var absoluteBookPath = layout.GetAbsolutePath(Path.Combine(bookDirectory, managedSourceName));
        var stagedBookPath = layout.GetAbsolutePath(Path.Combine(stagingDirectory, managedSourceName));
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
                stagedCoverPath = layout.GetAbsolutePath(Path.Combine(stagingDirectory, "cover.jpg"));
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
                        layout.GetAbsolutePath(Path.Combine(bookDirectory, "cover.jpg")),
                        overwrite: true);
                }
            }

            return (
                layout.ToRelativePath(absoluteBookPath),
                stagedCoverPath is null ? null : layout.ToRelativePath(Path.Combine(bookDirectory, "cover.jpg")),
                hash);
        }
        finally
        {
            if (Directory.Exists(stagingDirectory))
            {
                TryDeleteDirectory(stagingDirectory);
            }

            TryDeleteEmptyShardDirectory(bookDirectory);
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

        var bookDirectory = layout.ResolveExistingOrNewBookDirectory(bookId);
        layout.EnsureNoReparsePoints(bookDirectory);
        if (Directory.Exists(bookDirectory))
        {
            Directory.Delete(bookDirectory, recursive: true);
            TryDeleteEmptyShardDirectory(bookDirectory);
        }

        return Task.CompletedTask;
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var absolutePath = GetAbsolutePath(relativePath);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

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

    private void TryDeleteEmptyShardDirectory(string bookDirectory)
    {
        var shardDirectory = Path.GetDirectoryName(bookDirectory);
        if (shardDirectory is null ||
            Path.GetDirectoryName(shardDirectory) is not { } shardParent ||
            !string.Equals(
                Path.GetFullPath(shardParent),
                Path.GetFullPath(layout.BooksDirectory),
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            Directory.Delete(shardDirectory, recursive: false);
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

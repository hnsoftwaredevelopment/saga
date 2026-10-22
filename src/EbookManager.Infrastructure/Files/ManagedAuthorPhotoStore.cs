using EbookManager.Application.Authors;

namespace EbookManager.Infrastructure.Files;

public sealed class ManagedAuthorPhotoStore(string libraryRoot) : IAuthorPhotoStore
{
    private readonly ManagedLibraryStorageLayout layout = new(libraryRoot);
    private const int MaximumBytes = 10 * 1024 * 1024;

    public async Task<string> SaveJpegAsync(Guid authorId, byte[] jpeg, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(authorId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(jpeg);
        if (jpeg.Length is < 4 or > MaximumBytes || jpeg[0] != 0xff || jpeg[1] != 0xd8)
            throw new ArgumentException("Expected a bounded, processed JPEG.", nameof(jpeg));
        var id = authorId.ToString("N");
        var relative = $"authors/{id[..2]}/{id}-{Guid.NewGuid():N}.jpg";
        var path = Resolve(authorId, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        layout.EnsureNoReparsePoints(path);
        try
        {
            await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await stream.WriteAsync(jpeg, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        catch
        {
            // This unique path belongs only to the current operation.
            layout.EnsureNoReparsePoints(path);
            if (File.Exists(path)) File.Delete(path);
            throw;
        }
        return relative;
    }

    public async Task<byte[]?> ReadAsync(Guid authorId, string relativePath, CancellationToken cancellationToken)
    {
        var path = Resolve(authorId, relativePath);
        if (!File.Exists(path)) return null;
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        if (stream.Length is < 4 or > MaximumBytes) throw new InvalidDataException("Invalid photo size.");
        var bytes = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(bytes, cancellationToken);
        return bytes;
    }

    public Task DeleteAsync(Guid authorId, string relativePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Resolve(authorId, relativePath);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string Resolve(Guid authorId, string relativePath)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(authorId, Guid.Empty);
        var id = authorId.ToString("N");
        var prefix = $"authors/{id[..2]}/{id}-";
        if (!relativePath.StartsWith(prefix, StringComparison.Ordinal) ||
            !relativePath.EndsWith(".jpg", StringComparison.Ordinal) ||
            !Guid.TryParseExact(relativePath[prefix.Length..^4], "N", out _))
            throw new ArgumentException("Photo path does not belong to this author.", nameof(relativePath));
        var path = layout.GetAbsolutePath(relativePath);
        layout.EnsureNoReparsePoints(path);
        return path;
    }
}

using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;

namespace EbookManager.Application.Books;

internal static class MetadataSidecarWriter
{
    public static async Task WriteAsync(
        ILibraryFileStore fileStore,
        IMetadataSidecarStore metadataSidecarStore,
        BookMetadata metadata,
        IReadOnlyList<BookFile> files,
        CancellationToken cancellationToken)
    {
        var writtenDirectories = new HashSet<string>(
            OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal);
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var absolutePath = fileStore.GetAbsolutePath(file.RelativePath);
            var directory = Path.GetDirectoryName(absolutePath);
            if (directory is null || !writtenDirectories.Add(directory))
            {
                continue;
            }

            await metadataSidecarStore.WriteAsync(
                absolutePath,
                metadata,
                cancellationToken);
        }
    }
}

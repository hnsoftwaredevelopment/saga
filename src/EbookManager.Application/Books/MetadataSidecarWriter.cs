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
        Exception? firstFailure = null;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
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
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                firstFailure ??= exception;
            }
        }

        if (firstFailure is not null)
        {
            throw new IOException(firstFailure.Message, firstFailure);
        }
    }
}

using System.IO.Compression;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;

namespace EbookManager.Infrastructure.Metadata;

public sealed class CbzMetadataAdapter : IMetadataAdapter
{
    private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private readonly FallbackMetadataAdapter fallback = new();

    public bool CanHandle(EbookFormat format) => format == EbookFormat.Cbz;

    public async Task<MetadataReadResult> ReadAsync(
        string path,
        EbookFormat format,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fallbackResult = await fallback.ReadAsync(path, format, cancellationToken);

        try
        {
            await using var stream = new FileStream(
                Path.GetFullPath(path),
                new FileStreamOptions
                {
                    Access = FileAccess.Read,
                    Mode = FileMode.Open,
                    Share = FileShare.Read,
                    Options = FileOptions.Asynchronous | FileOptions.SequentialScan
                });

            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            var coverEntry = archive.Entries
                .Where(entry => !string.IsNullOrEmpty(entry.Name))
                .Where(entry => SupportedImageExtensions.Contains(Path.GetExtension(entry.FullName)))
                .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (coverEntry is null)
            {
                return fallbackResult;
            }

            using var coverStream = coverEntry.Open();
            using var memoryStream = new MemoryStream();
            await coverStream.CopyToAsync(memoryStream, cancellationToken);

            return new MetadataReadResult(
                new BookMetadata(
                    fallbackResult.Metadata.Title,
                    fallbackResult.Metadata.Authors,
                    fallbackResult.Metadata.Description,
                    fallbackResult.Metadata.Language,
                    fallbackResult.Metadata.Publisher,
                    fallbackResult.Metadata.PublicationDate,
                    fallbackResult.Metadata.Tags,
                    fallbackResult.Metadata.Series,
                    fallbackResult.Metadata.SeriesNumber,
                    fallbackResult.Metadata.Isbn,
                    memoryStream.ToArray()),
                fallbackResult.Warning);
        }
        catch (Exception exception) when (
            exception is IOException or InvalidDataException or NotSupportedException)
        {
            return fallbackResult with
            {
                Warning = $"Malformed CBZ archive: {exception.Message}"
            };
        }
    }

    public Task<MetadataWriteResult> WriteAsync(
        string path,
        EbookFormat format,
        BookMetadata metadata,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new MetadataWriteResult(
            MetadataWriteBackStatus.Unsupported,
            "CBZ write-back is not supported."));
    }
}

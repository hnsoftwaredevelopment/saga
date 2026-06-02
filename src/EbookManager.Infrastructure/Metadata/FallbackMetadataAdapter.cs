using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;

namespace EbookManager.Infrastructure.Metadata;

public sealed class FallbackMetadataAdapter : IMetadataAdapter
{
    public bool CanHandle(EbookFormat format) => EbookFormatExtensions.Supported.Contains(format);

    public Task<MetadataReadResult> ReadAsync(
        string path,
        EbookFormat format,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var metadata = ParseMetadata(path);
        return Task.FromResult(new MetadataReadResult(metadata));
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
            "Write-back is not supported for fallback metadata."));
    }

    private static BookMetadata ParseMetadata(string path)
    {
        var fileName = Path.GetFileName(path);
        if (fileName.EndsWith(".kepub.epub", StringComparison.OrdinalIgnoreCase))
        {
            fileName = fileName[..^".kepub.epub".Length];
        }
        else
        {
            fileName = Path.GetFileNameWithoutExtension(fileName);
        }

        var separatorIndex = fileName.IndexOf(" - ", StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            return new BookMetadata(fileName, ["Unknown"]);
        }

        var title = fileName[..separatorIndex].Trim();
        var author = fileName[(separatorIndex + 3)..].Trim();
        if (author.Length == 0)
        {
            author = "Unknown";
        }

        if (title.Length == 0)
        {
            title = fileName.Trim();
        }

        return new BookMetadata(title, [author]);
    }
}

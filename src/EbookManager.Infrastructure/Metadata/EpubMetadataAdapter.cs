using System.IO.Compression;
using System.Xml.Linq;
using System.Xml;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;

namespace EbookManager.Infrastructure.Metadata;

public sealed class EpubMetadataAdapter : IMetadataAdapter
{
    private readonly FallbackMetadataAdapter fallback = new();

    public bool CanHandle(EbookFormat format) => format is EbookFormat.Epub or EbookFormat.Kepub;

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
            var containerDocument = LoadXmlDocument(archive, "META-INF/container.xml")
                ?? throw new InvalidDataException("Missing EPUB container.");

            var rootfilePath = GetRootfilePath(containerDocument)
                ?? throw new InvalidDataException("Missing EPUB rootfile.");

            var opfDocument = LoadXmlDocument(archive, NormalizeZipPath(rootfilePath))
                ?? throw new InvalidDataException("Missing EPUB package.");

            var package = opfDocument.Root ?? throw new InvalidDataException("Missing EPUB package root.");
            var metadataElement = FindChild(package, "metadata")
                ?? throw new InvalidDataException("Missing EPUB metadata.");
            var manifestElement = FindChild(package, "manifest");

            var title = FirstElementValue(metadataElement, "title") ?? fallbackResult.Metadata.Title;
            var authors = FirstElementValues(metadataElement, "creator").ToArray();
            if (authors.Length == 0)
            {
                authors = fallbackResult.Metadata.Authors.ToArray();
            }

            var description = FirstElementValue(metadataElement, "description") ?? fallbackResult.Metadata.Description;
            var language = FirstElementValue(metadataElement, "language") ?? fallbackResult.Metadata.Language;
            var publisher = FirstElementValue(metadataElement, "publisher") ?? fallbackResult.Metadata.Publisher;
            var isbn = FirstIsbn(metadataElement) ?? fallbackResult.Metadata.Isbn;
            var coverBytes = TryGetCoverBytes(archive, rootfilePath, metadataElement, manifestElement)
                ?? fallbackResult.Metadata.CoverBytes;

            return new MetadataReadResult(
                new BookMetadata(
                    title,
                    authors,
                    description,
                    language,
                    publisher,
                    fallbackResult.Metadata.PublicationDate,
                    fallbackResult.Metadata.Tags,
                    fallbackResult.Metadata.Series,
                    fallbackResult.Metadata.SeriesNumber,
                    isbn,
                    coverBytes));
        }
        catch (Exception exception) when (
            exception is IOException or InvalidDataException or XmlException or ArgumentException or NotSupportedException)
        {
            return fallbackResult with
            {
                Warning = $"Malformed EPUB metadata: {exception.Message}"
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
            "EPUB write-back is not supported."));
    }

    private static XDocument? LoadXmlDocument(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(NormalizeZipPath(entryName));
        if (entry is null)
        {
            return null;
        }

        using var stream = entry.Open();
        return XDocument.Load(stream, LoadOptions.None);
    }

    private static string? GetRootfilePath(XDocument document) =>
        document
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "rootfile")
            ?.Attribute("full-path")
            ?.Value;

    private static XElement? FindChild(XElement element, string localName) =>
        element.Elements().FirstOrDefault(child => child.Name.LocalName == localName);

    private static string? FirstElementValue(XElement element, string localName) =>
        element
            .Elements()
            .FirstOrDefault(child => child.Name.LocalName == localName)
            ?.Value
            .Trim()
            is { Length: > 0 } value
            ? value
            : null;

    private static IEnumerable<string> FirstElementValues(XElement element, string localName) =>
        element
            .Elements()
            .Where(child => child.Name.LocalName == localName)
            .Select(child => child.Value.Trim())
            .Where(value => value.Length > 0);

    private static string? FirstIsbn(XElement metadataElement)
    {
        foreach (var identifier in metadataElement.Elements().Where(child => child.Name.LocalName == "identifier"))
        {
            var scheme = identifier
                .Attributes()
                .FirstOrDefault(attribute =>
                    attribute.Name.LocalName.Equals("scheme", StringComparison.OrdinalIgnoreCase)
                    && attribute.Value.Equals("ISBN", StringComparison.OrdinalIgnoreCase))
                ?.Value;

            if (!string.IsNullOrWhiteSpace(scheme))
            {
                var value = identifier.Value.Trim();
                if (value.Length > 0)
                {
                    return value;
                }
            }
        }

        return metadataElement
            .Elements()
            .FirstOrDefault(child => child.Name.LocalName == "identifier" && child.Value.Trim().Length > 0)
            ?.Value
            .Trim();
    }

    private static byte[]? TryGetCoverBytes(
        ZipArchive archive,
        string rootfilePath,
        XElement metadataElement,
        XElement? manifestElement)
    {
        if (manifestElement is null)
        {
            return null;
        }

        var coverId = metadataElement
            .Elements()
            .FirstOrDefault(child =>
                child.Name.LocalName == "meta" &&
                string.Equals(child.Attribute("name")?.Value, "cover", StringComparison.OrdinalIgnoreCase))
            ?.Attribute("content")
            ?.Value;

        if (!string.IsNullOrWhiteSpace(coverId))
        {
            var coverItem = manifestElement
                .Elements()
                .FirstOrDefault(child =>
                    child.Name.LocalName == "item" &&
                    string.Equals(child.Attribute("id")?.Value, coverId, StringComparison.OrdinalIgnoreCase));

            var coverBytes = ReadEntryBytes(
                archive,
                ResolveZipPath(rootfilePath, coverItem?.Attribute("href")?.Value));
            if (coverBytes is not null)
            {
                return coverBytes;
            }
        }

        var coverImageItem = manifestElement
            .Elements()
            .FirstOrDefault(child =>
                child.Name.LocalName == "item" &&
                HasToken(child.Attribute("properties")?.Value, "cover-image"));

        return ReadEntryBytes(
            archive,
            ResolveZipPath(rootfilePath, coverImageItem?.Attribute("href")?.Value));
    }

    private static byte[]? ReadEntryBytes(ZipArchive archive, string? entryName)
    {
        if (string.IsNullOrWhiteSpace(entryName))
        {
            return null;
        }

        var entry = archive.GetEntry(NormalizeZipPath(entryName));
        if (entry is null)
        {
            return null;
        }

        using var stream = entry.Open();
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    private static string ResolveZipPath(string rootfilePath, string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        var rootfileDirectory = Path.GetDirectoryName(NormalizeZipPath(rootfilePath)) ?? string.Empty;
        return NormalizeZipPath(Path.Combine(rootfileDirectory, relativePath));
    }

    private static string NormalizeZipPath(string path) =>
        path.Replace('\\', '/').TrimStart("./".ToCharArray());

    private static bool HasToken(string? values, string token) =>
        values?
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(value => value.Equals(token, StringComparison.OrdinalIgnoreCase)) == true;
}

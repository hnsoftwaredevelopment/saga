using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
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

            var rootfilePath = GetSingleRootfilePath(containerDocument);
            var opfDocument = LoadXmlDocument(archive, rootfilePath)
                ?? throw new InvalidDataException("Missing EPUB package.");

            var package = opfDocument.Root ?? throw new InvalidDataException("Missing EPUB package root.");
            var metadataElement = FindSingleChild(package, "metadata")
                ?? throw new InvalidDataException("Missing EPUB metadata.");
            var manifestElement = FindSingleChild(package, "manifest");

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
            var tags = ElementValues(metadataElement, "subject")
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var series = MetaContent(metadataElement, "calibre:series") ?? fallbackResult.Metadata.Series;
            var seriesNumber = ParseSeriesNumber(MetaContent(metadataElement, "calibre:series_index"))
                ?? fallbackResult.Metadata.SeriesNumber;
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
                    tags.Length > 0 ? tags : fallbackResult.Metadata.Tags,
                    series,
                    seriesNumber,
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

    private static XDocument? LoadXmlDocument(ZipArchive archive, string entryPath)
    {
        var entry = FindUniqueEntry(archive, entryPath);
        if (entry is null)
        {
            return null;
        }

        using var stream = entry.Open();
        return XDocument.Load(stream, LoadOptions.None);
    }

    private static string GetSingleRootfilePath(XDocument containerDocument)
    {
        var rootfiles = containerDocument
            .Descendants()
            .Where(element => element.Name.LocalName == "rootfile")
            .ToArray();

        if (rootfiles.Length != 1)
        {
            throw new InvalidDataException("EPUB container must contain exactly one rootfile.");
        }

        var rootfilePath = rootfiles[0].Attribute("full-path")?.Value;
        if (string.IsNullOrWhiteSpace(rootfilePath))
        {
            throw new InvalidDataException("EPUB rootfile is missing its full-path attribute.");
        }

        return CanonicalizeArchivePath(rootfilePath);
    }

    private static XElement? FindSingleChild(XElement element, string localName)
    {
        var children = element.Elements().Where(child => child.Name.LocalName == localName).ToArray();
        if (children.Length == 0)
        {
            return null;
        }

        if (children.Length > 1)
        {
            throw new InvalidDataException($"EPUB metadata contains multiple '{localName}' elements.");
        }

        return children[0];
    }

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
        ElementValues(element, localName);

    private static IEnumerable<string> ElementValues(XElement element, string localName) =>
        element
            .Elements()
            .Where(child => child.Name.LocalName == localName)
            .Select(child => child.Value.Trim())
            .Where(value => value.Length > 0);

    private static string? MetaContent(XElement metadataElement, string name) =>
        metadataElement
            .Elements()
            .Where(child => child.Name.LocalName == "meta")
            .FirstOrDefault(child => string.Equals(child.Attribute("name")?.Value, name, StringComparison.OrdinalIgnoreCase))
            ?.Attribute("content")
            ?.Value
            .Trim()
        is { Length: > 0 } value
            ? value
            : null;

    private static decimal? ParseSeriesNumber(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static string? FirstIsbn(XElement metadataElement)
    {
        foreach (var identifier in metadataElement.Elements().Where(child => child.Name.LocalName == "identifier"))
        {
            var scheme = identifier
                .Attributes()
                .FirstOrDefault(attribute =>
                    attribute.Name.LocalName.Equals("scheme", StringComparison.OrdinalIgnoreCase) &&
                    attribute.Value.Equals("ISBN", StringComparison.OrdinalIgnoreCase))
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

        var coverMeta = metadataElement
            .Elements()
            .Where(child =>
                child.Name.LocalName == "meta" &&
                string.Equals(child.Attribute("name")?.Value, "cover", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (coverMeta.Length > 1)
        {
            throw new InvalidDataException("EPUB metadata contains multiple cover references.");
        }

        if (coverMeta.Length == 1)
        {
            var coverId = coverMeta[0].Attribute("content")?.Value;
            if (string.IsNullOrWhiteSpace(coverId))
            {
                throw new InvalidDataException("EPUB cover reference is missing its content attribute.");
            }

            var coverItem = FindSingleManifestItemById(manifestElement, coverId);
            var href = coverItem.Attribute("href")?.Value;
            if (string.IsNullOrWhiteSpace(href))
            {
                throw new InvalidDataException("EPUB cover item is missing its href attribute.");
            }

            return ReadUniqueEntryBytes(archive, ResolveArchivePath(rootfilePath, href));
        }

        var coverImageItems = manifestElement
            .Elements()
            .Where(child =>
                child.Name.LocalName == "item" &&
                HasToken(child.Attribute("properties")?.Value, "cover-image"))
            .ToArray();

        if (coverImageItems.Length > 1)
        {
            throw new InvalidDataException("EPUB manifest contains multiple cover-image items.");
        }

        return coverImageItems.Length == 1
            ? ReadUniqueEntryBytes(
                archive,
                ResolveArchivePath(
                    rootfilePath,
                    coverImageItems[0].Attribute("href")?.Value
                    ?? throw new InvalidDataException("EPUB cover-image item is missing its href attribute.")))
            : null;
    }

    private static XElement FindSingleManifestItemById(XElement manifestElement, string id)
    {
        var matches = manifestElement
            .Elements()
            .Where(child =>
                child.Name.LocalName == "item" &&
                string.Equals(child.Attribute("id")?.Value, id, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length == 0)
        {
            throw new InvalidDataException($"EPUB manifest does not contain an item with id '{id}'.");
        }

        if (matches.Length > 1)
        {
            throw new InvalidDataException($"EPUB manifest contains multiple items with id '{id}'.");
        }

        return matches[0];
    }

    private static byte[]? ReadUniqueEntryBytes(ZipArchive archive, string? entryPath)
    {
        if (string.IsNullOrWhiteSpace(entryPath))
        {
            return null;
        }

        var entry = FindUniqueEntry(archive, entryPath);
        if (entry is null)
        {
            return null;
        }

        using var stream = entry.Open();
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    private static ZipArchiveEntry? FindUniqueEntry(ZipArchive archive, string entryPath)
    {
        var canonicalEntryPath = CanonicalizeArchivePath(entryPath);
        ZipArchiveEntry? match = null;

        foreach (var entry in archive.Entries)
        {
            var canonicalCandidate = CanonicalizeArchivePath(entry.FullName);
            if (!string.Equals(canonicalCandidate, canonicalEntryPath, StringComparison.Ordinal))
            {
                continue;
            }

            if (match is not null)
            {
                throw new InvalidDataException($"EPUB archive contains multiple entries for '{canonicalEntryPath}'.");
            }

            match = entry;
        }

        return match;
    }

    private static string ResolveArchivePath(string rootfilePath, string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        return CombineArchivePaths(rootfilePath, relativePath);
    }

    private static string CombineArchivePaths(string basePath, string relativePath)
    {
        var baseSegments = CanonicalizeArchivePath(basePath)
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (baseSegments.Count == 0)
        {
            throw new InvalidDataException("EPUB base path is invalid.");
        }

        baseSegments.RemoveAt(baseSegments.Count - 1);

        var relativeSegments = CanonicalizeRelativeArchivePath(relativePath)
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        baseSegments.AddRange(relativeSegments);
        return string.Join('/', baseSegments);
    }

    private static string CanonicalizeRelativeArchivePath(string path)
    {
        var canonicalPath = CanonicalizeArchivePath(path);
        if (path.StartsWith('/') ||
            path.StartsWith('\\') ||
            path.Contains(':'))
        {
            throw new InvalidDataException($"Unsafe EPUB archive path '{path}'.");
        }

        return canonicalPath;
    }

    private static string CanonicalizeArchivePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidDataException("EPUB archive path is empty.");
        }

        var normalized = path.Replace('\\', '/');
        if (normalized.StartsWith('/') ||
            normalized.StartsWith("//", StringComparison.Ordinal) ||
            normalized.Contains(':'))
        {
            throw new InvalidDataException($"Unsafe EPUB archive path '{path}'.");
        }

        var segments = new List<string>();
        foreach (var segment in normalized.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                throw new InvalidDataException($"Unsafe EPUB archive path '{path}'.");
            }

            segments.Add(segment);
        }

        if (segments.Count == 0)
        {
            throw new InvalidDataException($"Unsafe EPUB archive path '{path}'.");
        }

        return string.Join('/', segments);
    }

    private static bool HasToken(string? values, string token) =>
        values?
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(value => value.Equals(token, StringComparison.OrdinalIgnoreCase)) == true;
}

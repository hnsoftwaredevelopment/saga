using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;

namespace EbookManager.Infrastructure.Metadata;

public sealed class CalibreOpfMetadataSidecarStore
{
    public const string FileName = "metadata.opf";
    private const long MaxCoverSizeBytes = 10 * 1024 * 1024;

    public async Task<MetadataReadResult?> TryReadAsync(
        string bookFilePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directory = Path.GetDirectoryName(Path.GetFullPath(bookFilePath));
        if (string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        var opfPath = Path.Combine(directory, FileName);
        if (!File.Exists(opfPath))
        {
            return null;
        }

        try
        {
            await using var stream = new FileStream(
                opfPath,
                new FileStreamOptions
                {
                    Access = FileAccess.Read,
                    Mode = FileMode.Open,
                    Share = FileShare.Read,
                    Options = FileOptions.Asynchronous | FileOptions.SequentialScan
                });

            var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
            var metadataElement = document
                .Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "metadata");

            if (metadataElement is null)
            {
                return new MetadataReadResult(
                    FallbackMetadata(bookFilePath),
                    "Calibre OPF metadata section is missing.");
            }

            var title = FirstElementValue(metadataElement, "title")
                ?? Path.GetFileNameWithoutExtension(bookFilePath);
            var authors = ElementValues(metadataElement, "creator").ToArray();
            if (authors.Length == 0)
            {
                authors = ["Unknown"];
            }

            var tags = ElementValues(metadataElement, "subject")
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            return new MetadataReadResult(
                new BookMetadata(
                    title,
                    authors,
                    FirstElementValue(metadataElement, "description"),
                    FirstElementValue(metadataElement, "language"),
                    FirstElementValue(metadataElement, "publisher"),
                    MetadataDateParser.ParseDate(FirstElementValue(metadataElement, "date")),
                    tags.Length > 0 ? tags : null,
                    MetaContent(metadataElement, "calibre:series"),
                    ParseSeriesNumber(MetaContent(metadataElement, "calibre:series_index")),
                    FirstIsbn(metadataElement),
                    await TryReadCoverAsync(directory, cancellationToken)));
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or XmlException or InvalidDataException)
        {
            return new MetadataReadResult(
                FallbackMetadata(bookFilePath),
            $"Calibre OPF ignored: {exception.GetType().Name}");
        }
    }

    private static async Task<byte[]?> TryReadCoverAsync(
        string directory,
        CancellationToken cancellationToken)
    {
        var coverPath = Path.Combine(directory, "cover.jpg");
        if (!File.Exists(coverPath))
        {
            return null;
        }

        try
        {
            var fileInfo = new FileInfo(coverPath);
            if (fileInfo.Length <= 0 || fileInfo.Length > MaxCoverSizeBytes)
            {
                return null;
            }

            return await File.ReadAllBytesAsync(fileInfo.FullName, cancellationToken);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or FileNotFoundException or PathTooLongException)
        {
            return null;
        }
    }

    private static BookMetadata FallbackMetadata(string bookFilePath) =>
        new(Path.GetFileNameWithoutExtension(bookFilePath), ["Unknown"]);

    private static string? FirstElementValue(XElement element, string localName) =>
        ElementValues(element, localName).FirstOrDefault();

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
            .Trim() is { Length: > 0 } value
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
            var value = identifier.Value.Trim();
            if (value.Length == 0)
            {
                continue;
            }

            var scheme = identifier
                .Attributes()
                .FirstOrDefault(attribute =>
                    attribute.Name.LocalName.Equals("scheme", StringComparison.OrdinalIgnoreCase) &&
                    attribute.Value.Equals("ISBN", StringComparison.OrdinalIgnoreCase))
                ?.Value;

            if (!string.IsNullOrWhiteSpace(scheme) || value.Contains("isbn", StringComparison.OrdinalIgnoreCase))
            {
                return value.Replace("ISBN:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            }
        }

        return metadataElement
            .Elements()
            .FirstOrDefault(child => child.Name.LocalName == "identifier" && child.Value.Trim().Length > 0)
            ?.Value
            .Trim();
    }
}

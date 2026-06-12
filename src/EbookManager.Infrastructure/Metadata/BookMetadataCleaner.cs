using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using EbookManager.Domain.Books;

namespace EbookManager.Infrastructure.Metadata;

public static partial class BookMetadataCleaner
{
    public static BookMetadata Clean(BookMetadata metadata)
    {
        var title = metadata.Title.Trim();
        var series = NormalizeBlank(metadata.Series);
        var seriesNumber = metadata.SeriesNumber;

        if (TryParseBracketedSeries(title, out var parsedTitle, out var parsedSeries, out var parsedNumber))
        {
            title = parsedTitle;
            series ??= parsedSeries;
            seriesNumber ??= parsedNumber;
        }

        var authors = metadata.Authors
            .Select(NormalizeAuthor)
            .Where(author => author.Length > 0)
            .ToArray();

        if (authors.Length == 0)
        {
            authors = ["Unknown"];
        }

        return new BookMetadata(
            title,
            authors,
            CleanDescription(metadata.Description),
            NormalizeBlank(metadata.Language),
            NormalizeBlank(metadata.Publisher),
            metadata.PublicationDate,
            metadata.Tags?
                .Select(tag => tag.Trim())
                .Where(tag => tag.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            series,
            seriesNumber,
            NormalizeBlank(metadata.Isbn),
            metadata.CoverBytes);
    }

    private static bool TryParseBracketedSeries(
        string title,
        out string parsedTitle,
        out string parsedSeries,
        out decimal parsedNumber)
    {
        parsedTitle = title;
        parsedSeries = string.Empty;
        parsedNumber = 0;

        var match = BracketedTitleRegex().Match(title);
        if (!match.Success)
        {
            return false;
        }

        var seriesPart = match.Groups["series"].Value.Trim();
        var numberPart = match.Groups["number"].Value.Trim();
        var titlePart = match.Groups["title"].Value.Trim();

        if (titlePart.Length == 0 ||
            seriesPart.Length == 0 ||
            !decimal.TryParse(numberPart, NumberStyles.Number, CultureInfo.InvariantCulture, out parsedNumber))
        {
            return false;
        }

        parsedTitle = titlePart;
        parsedSeries = seriesPart;
        return true;
    }

    private static string NormalizeAuthor(string author)
    {
        var trimmed = author.Trim();
        var commaIndex = trimmed.IndexOf(',', StringComparison.Ordinal);
        if (commaIndex < 0 || commaIndex != trimmed.LastIndexOf(','))
        {
            return trimmed;
        }

        var lastName = trimmed[..commaIndex].Trim();
        var firstName = trimmed[(commaIndex + 1)..].Trim();
        return lastName.Length > 0 && firstName.Length > 0
            ? $"{firstName} {lastName}"
            : trimmed;
    }

    private static string? NormalizeBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static string? CleanDescription(string? value)
    {
        var trimmed = NormalizeBlank(value);
        if (trimmed is null)
        {
            return null;
        }

        if (!LooksLikeHtml(trimmed))
        {
            return trimmed;
        }

        var text = BreakRegex().Replace(trimmed, "\n");
        text = ParagraphBoundaryRegex().Replace(text, "\n\n");
        text = HtmlTagRegex().Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text);
        text = HorizontalWhitespaceRegex().Replace(text, " ");
        text = LineWhitespaceRegex().Replace(text, "\n");
        text = ExcessiveNewlineRegex().Replace(text, "\n\n");
        return NormalizeBlank(text);
    }

    private static bool LooksLikeHtml(string value) =>
        HtmlTagRegex().IsMatch(value) || value.Contains("&amp;", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"^\[(?<series>.+?)\s+(?<number>\d+(?:\.\d+)?)\]\s*[-:\s]\s*(?<title>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex BracketedTitleRegex();

    [GeneratedRegex(@"<\s*br\s*/?\s*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BreakRegex();

    [GeneratedRegex(@"</\s*p\s*>|</\s*div\s*>|</\s*section\s*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ParagraphBoundaryRegex();

    [GeneratedRegex(@"<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"[^\S\r\n]+", RegexOptions.CultureInvariant)]
    private static partial Regex HorizontalWhitespaceRegex();

    [GeneratedRegex(@"[ \t]*\r?\n[ \t]*", RegexOptions.CultureInvariant)]
    private static partial Regex LineWhitespaceRegex();

    [GeneratedRegex(@"\n{3,}", RegexOptions.CultureInvariant)]
    private static partial Regex ExcessiveNewlineRegex();
}

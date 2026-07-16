using System.Net;
using System.Text.RegularExpressions;

namespace EbookManager.Application.Metadata;

public static partial class DescriptionTextCleaner
{
    public static string? Clean(string? value)
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

    private static string? NormalizeBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool LooksLikeHtml(string value) =>
        HtmlTagRegex().IsMatch(value) || value.Contains("&amp;", StringComparison.OrdinalIgnoreCase);

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

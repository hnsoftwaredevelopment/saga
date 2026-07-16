using System.Globalization;
using System.Text;

namespace EbookManager.Application.Metadata;

public static class LanguageDisplayService
{
    private static readonly IReadOnlyDictionary<string, string> SupportedLanguageNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["english"] = "en",
        ["engels"] = "en",
        ["englisch"] = "en",
        ["anglais"] = "en",
        ["ingles"] = "en",
        ["inglese"] = "en",

        ["dutch"] = "nl",
        ["nederlands"] = "nl",
        ["niederlandisch"] = "nl",
        ["neerlandais"] = "nl",
        ["neerlandes"] = "nl",
        ["olandese"] = "nl",

        ["german"] = "de",
        ["duits"] = "de",
        ["deutsch"] = "de",
        ["allemand"] = "de",
        ["aleman"] = "de",
        ["tedesco"] = "de",

        ["french"] = "fr",
        ["frans"] = "fr",
        ["franzosisch"] = "fr",
        ["francais"] = "fr",
        ["frances"] = "fr",
        ["francese"] = "fr",

        ["spanish"] = "es",
        ["spaans"] = "es",
        ["spanisch"] = "es",
        ["espagnol"] = "es",
        ["espanol"] = "es",
        ["spagnolo"] = "es",

        ["italian"] = "it",
        ["italiaans"] = "it",
        ["italienisch"] = "it",
        ["italien"] = "it",
        ["italiano"] = "it"
    };

    public static string? FilterKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Equals("eng", StringComparison.OrdinalIgnoreCase))
        {
            return "en";
        }

        if (SupportedLanguageNames.TryGetValue(NormalizeLanguageNameKey(normalized), out var supportedLanguageCode))
        {
            return supportedLanguageCode;
        }

        try
        {
            return CultureInfo.GetCultureInfo(normalized).TwoLetterISOLanguageName;
        }
        catch (CultureNotFoundException)
        {
            return normalized;
        }
    }

    public static string DisplayName(string value)
    {
        var normalized = FilterKey(value) ?? value.Trim();
        if (normalized.Length == 0)
        {
            return normalized;
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(normalized);
            var languageOnly = CultureInfo.GetCultureInfo(culture.TwoLetterISOLanguageName);
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(languageOnly.DisplayName);
        }
        catch (CultureNotFoundException)
        {
            return value;
        }
    }

    private static string NormalizeLanguageNameKey(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

using System.Globalization;

namespace EbookManager.Application.Metadata;

public static class LanguageDisplayService
{
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
}

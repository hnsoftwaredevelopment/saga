using System.Globalization;

namespace EbookManager.Application.Metadata;

public static class MetadataQualityLanguageRules
{
    public static string? Normalize(string? language)
    {
        var normalized = LanguageDisplayService.FilterKey(language);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        try
        {
            return CultureInfo.GetCultureInfo(normalized)
                .TwoLetterISOLanguageName
                .ToLowerInvariant();
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }
}

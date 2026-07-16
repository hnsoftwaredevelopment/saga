using System.Globalization;

namespace EbookManager.Application.Metadata;

public static class MetadataDateParser
{
    public static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length >= 10 &&
            DateOnly.TryParseExact(
                normalized[..10],
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var isoDate))
        {
            return isoDate;
        }

        return DateOnly.TryParse(
            normalized,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
                ? parsed
                : null;
    }
}

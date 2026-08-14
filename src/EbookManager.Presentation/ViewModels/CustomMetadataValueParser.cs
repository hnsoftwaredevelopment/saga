using EbookManager.Domain.CustomMetadata;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

internal static class CustomMetadataValueParser
{
    public static string? Format(CustomMetadataFieldType type, CustomMetadataValue? value) =>
        type switch
        {
            CustomMetadataFieldType.Number => value?.NumberValue?.ToString("0.#############################", CultureInfo.CurrentCulture),
            CustomMetadataFieldType.Date => value?.DateValue?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            CustomMetadataFieldType.Boolean => value?.BooleanValue?.ToString(),
            _ => value?.TextValue
        };

    public static CustomMetadataValue Create(Guid bookId, Guid fieldId, string name, CustomMetadataFieldType type, string? valueText) =>
        type switch
        {
            CustomMetadataFieldType.Text or CustomMetadataFieldType.SingleSelect or CustomMetadataFieldType.MultiSelect =>
                new CustomMetadataValue(bookId, fieldId, TextValue: NormalizeBlank(valueText)),
            CustomMetadataFieldType.Number =>
                TryParseNumber(valueText, out var number)
                    ? new CustomMetadataValue(bookId, fieldId, NumberValue: number)
                    : throw new FormatException($"CustomMetadataValidationNumber|{name}"),
            CustomMetadataFieldType.Date =>
                TryParseDate(valueText, out var date)
                    ? new CustomMetadataValue(bookId, fieldId, DateValue: date)
                    : throw new FormatException($"CustomMetadataValidationDate|{name}"),
            CustomMetadataFieldType.Boolean =>
                TryParseBoolean(valueText, out var boolean)
                    ? new CustomMetadataValue(bookId, fieldId, BooleanValue: boolean)
                    : throw new FormatException($"CustomMetadataValidationBoolean|{name}"),
            _ => throw new InvalidOperationException($"Unsupported custom metadata field type '{type}'.")
        };

    public static bool TryFormatValidationMessage(
        Exception exception,
        Func<string, string> localize,
        out string message)
    {
        var parts = exception.Message.Split('|', 2);
        if (parts.Length == 2 && parts[0].StartsWith("CustomMetadataValidation", StringComparison.Ordinal))
        {
            message = string.Format(CultureInfo.CurrentCulture, localize(parts[0]), parts[1]);
            return true;
        }

        message = exception.Message;
        return !string.IsNullOrWhiteSpace(message);
    }

    public static IReadOnlyList<string> SplitList(string? value, bool distinct = false)
    {
        var values = (value ?? string.Empty)
            .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return distinct
            ? values.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            : values;
    }

    public static IReadOnlyList<string>? SplitNullableList(string? value, bool distinct = false)
    {
        var values = SplitList(value, distinct);
        return values.Count == 0 ? null : values;
    }

    public static string? NormalizeBlank(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static bool TryParseNumber(string? value, out decimal result) =>
        decimal.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result) ||
        decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    private static bool TryParseBoolean(string? value, out bool result)
    {
        var normalized = NormalizeBlank(value);
        if (normalized is null)
        {
            result = false;
            return false;
        }

        if (bool.TryParse(normalized, out result))
        {
            return true;
        }

        if (string.Equals(normalized, "yes", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "ja", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "oui", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "si", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "sí", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "sì", StringComparison.OrdinalIgnoreCase) ||
            normalized == "1")
        {
            result = true;
            return true;
        }

        if (string.Equals(normalized, "no", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "false", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "nee", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "nein", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "non", StringComparison.OrdinalIgnoreCase) ||
            normalized == "0")
        {
            result = false;
            return true;
        }

        result = false;
        return false;
    }

    private static bool TryParseDate(string? value, out DateOnly result)
    {
        if (DateOnly.TryParse(value, CultureInfo.CurrentCulture, out result))
        {
            return true;
        }

        return DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);
    }
}

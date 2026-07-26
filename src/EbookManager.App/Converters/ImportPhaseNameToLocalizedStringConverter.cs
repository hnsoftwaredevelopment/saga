using System.Globalization;
using System.Windows.Data;
using EbookManager.App.Localization;

namespace EbookManager.App.Converters;

public sealed class ImportPhaseNameToLocalizedStringConverter : IValueConverter
{
    private static readonly IReadOnlyDictionary<string, string> ResourceKeys =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["local"] = "ImportPhaseLocal",
            ["size"] = "ImportPhaseSize",
            ["hash"] = "ImportPhaseHash",
            ["meta"] = "ImportPhaseMetadata",
            ["dup"] = "ImportPhaseDuplicate",
            ["copy"] = "ImportPhaseCopy",
            ["db"] = "ImportPhaseDatabase",
            ["cleanup"] = "ImportPhaseCleanup"
        };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string phaseName || phaseName.Length == 0)
        {
            return string.Empty;
        }

        return ResourceKeys.TryGetValue(phaseName, out var resourceKey)
            ? LocalizedStrings.Current[resourceKey]
            : phaseName;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

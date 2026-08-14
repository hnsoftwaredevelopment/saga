using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Converters;

public sealed class LibraryColumnGridLengthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var (option, width) = ParseParameter(parameter);
        var key = LibraryColumnKey.FromStandard(option);
        return TryGetWidth(value, key, width, out var actualWidth)
            ? new GridLength(actualWidth)
            : new GridLength(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;

    private static (LibraryColumnOption Option, double Width) ParseParameter(object parameter)
    {
        var parts = parameter?.ToString()?.Split('|', 2, StringSplitOptions.TrimEntries) ?? [];
        var option = parts.Length > 0 &&
            Enum.TryParse<LibraryColumnOption>(parts[0], ignoreCase: true, out var parsedOption)
                ? parsedOption
                : LibraryColumnOption.Title;
        var width = parts.Length > 1 &&
            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedWidth)
                ? parsedWidth
                : 120d;

        return (option, width);
    }

    private static bool TryGetWidth(
        object value,
        LibraryColumnKey key,
        double defaultWidth,
        out double width)
    {
        width = defaultWidth;
        if (value is LibraryColumnLayoutSnapshot snapshot)
        {
            if (!snapshot.VisibleColumns.Contains(key))
            {
                return false;
            }

            if (snapshot.ColumnWidths.TryGetValue(key, out var savedWidth) &&
                double.IsFinite(savedWidth) &&
                savedWidth > 0)
            {
                width = savedWidth;
            }

            return true;
        }

        return value is IEnumerable values && values.Cast<object>().OfType<LibraryColumnKey>().Contains(key);
    }
}

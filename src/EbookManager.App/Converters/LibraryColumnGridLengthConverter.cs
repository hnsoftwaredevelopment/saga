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
        return ContainsOption(value, option)
            ? new GridLength(width)
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

    private static bool ContainsOption(object value, LibraryColumnOption option) =>
        value is IEnumerable values && values.Cast<object>().OfType<LibraryColumnOption>().Contains(option);
}

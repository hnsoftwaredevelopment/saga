using System.Globalization;
using System.Windows.Data;
using EbookManager.App.Localization;

namespace EbookManager.App.Converters;

public sealed class LocalizationKeyToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string key && key.Length > 0
            ? LocalizedStrings.Current[key]
            : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

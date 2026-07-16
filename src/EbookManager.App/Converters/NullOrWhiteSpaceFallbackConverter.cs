using System.Globalization;
using System.Windows.Data;

namespace EbookManager.App.Converters;

public sealed class NullOrWhiteSpaceFallbackConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var value = values.Length > 0 ? values[0] as string : null;
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return values.Length > 1 && values[1] is string fallback
            ? fallback
            : string.Empty;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        targetTypes.Select(_ => Binding.DoNothing).ToArray();
}

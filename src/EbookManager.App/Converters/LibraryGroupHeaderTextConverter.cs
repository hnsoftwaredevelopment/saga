using System.Globalization;
using System.Windows.Data;
using EbookManager.App.Localization;

namespace EbookManager.App.Converters;

public sealed class LibraryGroupHeaderTextConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var header = values.ElementAtOrDefault(0)?.ToString() ?? string.Empty;
        var count = values.ElementAtOrDefault(1) is int intValue ? intValue : 0;
        var resourceKey = count == 1 ? "BookSingular" : "BookCount";
        var countText = $"{count} {LocalizedStrings.Current[resourceKey]}";
        return string.IsNullOrWhiteSpace(header)
            ? countText
            : $"{header} - {countText}";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

using EbookManager.App.Localization;
using EbookManager.Domain.Books;
using System.Globalization;
using System.Windows.Data;

namespace EbookManager.App.Converters;

public sealed class ReadingStatusToLocalizedStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ReadingStatus status
            ? LocalizedStrings.Current[status.ToString()]
            : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

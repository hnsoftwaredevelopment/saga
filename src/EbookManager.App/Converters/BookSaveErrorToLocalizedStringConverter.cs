using System.Globalization;
using System.Windows.Data;
using EbookManager.App.Localization;

namespace EbookManager.App.Converters;

public sealed class BookSaveErrorToLocalizedStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string message || string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        return message switch
        {
            "A book with the same title and author already exists." =>
                LocalizedStrings.Current["BookSaveConflict"],
            "The changes could not be saved." =>
                LocalizedStrings.Current["BookSaveFailed"],
            _ => message
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

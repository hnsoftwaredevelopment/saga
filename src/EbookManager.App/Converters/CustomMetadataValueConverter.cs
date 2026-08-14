using System.Globalization;
using System.Windows.Data;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Converters;

public sealed class CustomMetadataValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is BookRowViewModel row &&
        Guid.TryParse(parameter?.ToString(), out var fieldId)
            ? row.GetCustomMetadataValue(fieldId)
            : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

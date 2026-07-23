using System.Globalization;
using System.Windows.Data;
using EbookManager.App.Localization;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Converters;

public sealed class FormatExportStatusToLocalizedStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not BookFormatExportStatusMessage status)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(status.Message))
        {
            return status.Message;
        }

        var template = LocalizedStrings.Current[status.ResourceKey];
        return string.Format(CultureInfo.CurrentCulture, template, status.FormatText, status.FolderName);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

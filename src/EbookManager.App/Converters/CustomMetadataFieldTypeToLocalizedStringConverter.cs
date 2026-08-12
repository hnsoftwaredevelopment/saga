using System.Globalization;
using System.Windows.Data;
using EbookManager.App.Localization;
using EbookManager.Domain.CustomMetadata;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Converters;

public sealed class CustomMetadataFieldTypeToLocalizedStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CustomMetadataFieldTypeOption option)
        {
            return LocalizedStrings.Current[option.ResourceKey];
        }

        if (value is not CustomMetadataFieldType type)
        {
            return string.Empty;
        }

        var key = type switch
        {
            CustomMetadataFieldType.Text => "CustomMetadataFieldTypeText",
            CustomMetadataFieldType.Number => "CustomMetadataFieldTypeNumber",
            CustomMetadataFieldType.Date => "CustomMetadataFieldTypeDate",
            CustomMetadataFieldType.Boolean => "CustomMetadataFieldTypeBoolean",
            CustomMetadataFieldType.SingleSelect => "CustomMetadataFieldTypeSingleSelect",
            CustomMetadataFieldType.MultiSelect => "CustomMetadataFieldTypeMultiSelect",
            _ => "Type"
        };

        return LocalizedStrings.Current[key];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

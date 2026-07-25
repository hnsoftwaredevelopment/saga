using System.Globalization;
using System.Windows.Data;
using EbookManager.App.Localization;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Converters;

public sealed class LibraryGroupOptionToLocalizedStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not LibraryGroupOption option)
        {
            return string.Empty;
        }

        var key = option switch
        {
            LibraryGroupOption.Author => "Authors",
            LibraryGroupOption.Series => "Series",
            LibraryGroupOption.Tag => "Tags",
            LibraryGroupOption.Language => "Language",
            LibraryGroupOption.Status => "Status",
            LibraryGroupOption.Format => "Type",
            _ => "NoGrouping"
        };

        return LocalizedStrings.Current[key];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

using System.Globalization;
using System.Windows.Data;
using EbookManager.App.Localization;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Converters;

public sealed class LibraryColumnOptionToLocalizedStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LibraryColumnChoiceViewModel choice)
        {
            return Convert(choice.Option, targetType, parameter, culture);
        }

        if (value is not LibraryColumnOption option)
        {
            return string.Empty;
        }

        var key = option switch
        {
            LibraryColumnOption.Cover => "Cover",
            LibraryColumnOption.Title => "Title",
            LibraryColumnOption.Authors => "Authors",
            LibraryColumnOption.Format => "Type",
            LibraryColumnOption.Series => "Series",
            LibraryColumnOption.SeriesNumber => "SeriesNumber",
            LibraryColumnOption.Status => "Status",
            LibraryColumnOption.Language => "Language",
            LibraryColumnOption.Publisher => "Publisher",
            LibraryColumnOption.PublicationDate => "PublicationDate",
            LibraryColumnOption.Tags => "Tags",
            LibraryColumnOption.Isbn => "Isbn",
            LibraryColumnOption.Description => "Description",
            LibraryColumnOption.DateAdded => "DateAdded",
            LibraryColumnOption.LastModified => "LastModified",
            LibraryColumnOption.EReader => "EReader",
            _ => "Columns"
        };

        return LocalizedStrings.Current[key];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

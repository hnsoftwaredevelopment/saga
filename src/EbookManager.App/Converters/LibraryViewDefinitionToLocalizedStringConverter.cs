using System.Globalization;
using System.Windows.Data;
using EbookManager.App.Localization;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Converters;

public sealed class LibraryViewDefinitionToLocalizedStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not LibraryViewDefinitionViewModel definition)
        {
            return string.Empty;
        }

        if (!definition.IsBuiltIn)
        {
            return definition.Name;
        }

        var key = definition.BaseView switch
        {
            LibraryView.Bookshelf => "BookshelfView",
            LibraryView.Detailed => "DetailedView",
            LibraryView.List => "ListView",
            _ => "View"
        };

        return LocalizedStrings.Current[key];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

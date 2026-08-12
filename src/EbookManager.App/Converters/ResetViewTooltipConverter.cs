using System.Globalization;
using System.Windows.Data;
using EbookManager.App.Localization;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Converters;

public sealed class ResetViewTooltipConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var template = values.Length > 0 ? values[0]?.ToString() : null;
        var viewName = values.Length > 1
            ? ViewName(values[1])
            : string.Empty;

        return string.Format(
            CultureInfo.CurrentCulture,
            string.IsNullOrWhiteSpace(template) ? "{0}" : template,
            viewName);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        targetTypes.Select(_ => Binding.DoNothing).ToArray();

    private static string ViewResourceKey(LibraryView view) =>
        view switch
        {
            LibraryView.Bookshelf => "BookshelfView",
            LibraryView.Detailed => "DetailedView",
            LibraryView.List => "ListView",
            _ => "DetailedView"
        };

    private static string ViewName(object? value) =>
        value switch
        {
            LibraryViewDefinitionViewModel { IsBuiltIn: false } definition => definition.Name,
            LibraryViewDefinitionViewModel definition => LocalizedStrings.Current[ViewResourceKey(definition.BaseView)],
            LibraryView view => LocalizedStrings.Current[ViewResourceKey(view)],
            _ => string.Empty
        };
}

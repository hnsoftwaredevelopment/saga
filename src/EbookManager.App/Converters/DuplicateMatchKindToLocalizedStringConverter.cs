using System.Globalization;
using System.Windows.Data;
using EbookManager.App.Localization;
using EbookManager.Application.Books;

namespace EbookManager.App.Converters;

public sealed class DuplicateMatchKindToLocalizedStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DuplicateCandidateMatchKind matchKind)
        {
            return string.Empty;
        }

        var key = matchKind switch
        {
            DuplicateCandidateMatchKind.AuthorOverlap => "DuplicateMatchAuthorOverlap",
            DuplicateCandidateMatchKind.TitleOnly => "DuplicateMatchTitleOnly",
            _ => string.Empty
        };

        return string.IsNullOrEmpty(key) ? matchKind.ToString() : LocalizedStrings.Current[key];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

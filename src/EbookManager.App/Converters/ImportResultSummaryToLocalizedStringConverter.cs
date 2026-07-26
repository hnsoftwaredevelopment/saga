using System.Globalization;
using System.Windows.Data;
using EbookManager.App.Localization;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Converters;

public sealed class ImportResultSummaryToLocalizedStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ImportResultViewModel result)
        {
            return string.Empty;
        }

        return string.Format(
            CultureInfo.CurrentCulture,
            LocalizedStrings.Current["ImportResultsSummaryFormat"],
            result.TotalCount,
            result.AddedCount,
            result.SkippedCount,
            result.FailedCount);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

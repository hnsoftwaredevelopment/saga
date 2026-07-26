using System.Globalization;
using System.Windows.Data;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Converters;

public sealed class ImportPhaseSummaryToLocalizedStringConverter : IValueConverter
{
    private static readonly ImportPhaseNameToLocalizedStringConverter PhaseNameConverter = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<ImportPhaseSummaryViewModel> summaries)
        {
            return string.Empty;
        }

        return string.Join(
            "; ",
            summaries.Select(summary =>
            {
                var phaseName = PhaseNameConverter.Convert(summary.Name, typeof(string), null, culture);
                return $"{phaseName} {summary.DurationText} ({summary.PercentageText})";
            }));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

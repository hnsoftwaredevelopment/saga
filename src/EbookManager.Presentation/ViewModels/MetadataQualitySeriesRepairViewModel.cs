using CommunityToolkit.Mvvm.ComponentModel;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class MetadataQualitySeriesRepairViewModel : ObservableObject
{
    private readonly IReadOnlyList<string> knownSeries;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(NormalizedSeries))]
    private string seriesText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<string> suggestions;

    public MetadataQualitySeriesRepairViewModel(
        string bookTitle,
        decimal seriesNumber,
        IEnumerable<string?> knownSeries)
    {
        ArgumentNullException.ThrowIfNull(knownSeries);

        BookTitle = bookTitle;
        SeriesNumber = seriesNumber;
        this.knownSeries = knownSeries
            .Where(series => !string.IsNullOrWhiteSpace(series))
            .Select(series => series!.Trim())
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(series => series, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        suggestions = this.knownSeries;
    }

    public string BookTitle { get; }
    public decimal SeriesNumber { get; }
    public string? NormalizedSeries => string.IsNullOrWhiteSpace(SeriesText) ? null : SeriesText.Trim();
    public bool CanSave => NormalizedSeries is not null;

    public void UseSuggestion(string? series)
    {
        if (!string.IsNullOrWhiteSpace(series))
        {
            SeriesText = series;
        }
    }

    partial void OnSeriesTextChanged(string value) => Suggestions = FilterSuggestions(value);

    private IReadOnlyList<string> FilterSuggestions(string value)
    {
        var query = value.Trim();
        if (query.Length == 0)
        {
            return knownSeries;
        }

        return knownSeries
            .Where(series => series.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(series => series.StartsWith(query, StringComparison.CurrentCultureIgnoreCase) ? 0 : 1)
            .ThenBy(series => series, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }
}

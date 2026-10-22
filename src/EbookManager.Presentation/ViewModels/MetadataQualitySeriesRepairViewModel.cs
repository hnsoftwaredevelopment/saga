using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;

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
        IReadOnlyCollection<(string BookTitle, decimal SeriesNumber)> books,
        IEnumerable<string?> knownSeries,
        Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(books);
        ArgumentNullException.ThrowIfNull(knownSeries);
        ArgumentNullException.ThrowIfNull(localize);

        if (books.Count == 0)
        {
            throw new ArgumentException("At least one book is required.", nameof(books));
        }

        AffectedBookCount = books.Count;
        var singleBook = AffectedBookCount == 1 ? books.Single() : default;
        BookTitle = AffectedBookCount == 1 ? singleBook.BookTitle : null;
        SeriesNumber = AffectedBookCount == 1 ? singleBook.SeriesNumber : null;
        ContextLabel = localize(AffectedBookCount == 1 ? "Title" : "MetadataMultiEditSelectedBooks");
        ContextText = BookTitle ?? string.Format(
            CultureInfo.CurrentCulture,
            localize("MetadataQualitySeriesRepairBulkBookContext"),
            AffectedBookCount);
        SeriesNumberLabel = localize(AffectedBookCount == 1
            ? "SeriesNumber"
            : "MetadataQualitySeriesRepairBulkSeriesNumbersLabel");
        SeriesNumberText = SeriesNumber?.ToString(CultureInfo.CurrentCulture) ??
            localize("MetadataQualitySeriesRepairBulkSeriesNumbersHelp");
        SaveButtonText = AffectedBookCount == 1
            ? localize("MetadataQualitySeriesRepairSave")
            : string.Format(
                CultureInfo.CurrentCulture,
                localize("MetadataQualitySeriesRepairBulkSave"),
                AffectedBookCount);
        this.knownSeries = knownSeries
            .Where(series => !string.IsNullOrWhiteSpace(series))
            .Select(series => series!.Trim())
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(series => series, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        suggestions = this.knownSeries;
    }

    public int AffectedBookCount { get; }
    public string? BookTitle { get; }
    public decimal? SeriesNumber { get; }
    public string ContextLabel { get; }
    public string ContextText { get; }
    public string SeriesNumberLabel { get; }
    public string SeriesNumberText { get; }
    public string SaveButtonText { get; }
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

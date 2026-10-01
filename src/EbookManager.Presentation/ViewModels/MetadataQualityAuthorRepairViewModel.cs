using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Application.Metadata;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class MetadataQualityAuthorRepairViewModel : ObservableObject
{
    private readonly IReadOnlyList<string> knownAuthors;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(NormalizedAuthor))]
    private string authorText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<string> suggestions;

    public MetadataQualityAuthorRepairViewModel(
        string bookTitle,
        IEnumerable<string> knownAuthors)
        : this([bookTitle], knownAuthors, key => key)
    {
    }

    public MetadataQualityAuthorRepairViewModel(
        IReadOnlyCollection<string> bookTitles,
        IEnumerable<string> knownAuthors,
        Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(bookTitles);
        ArgumentNullException.ThrowIfNull(knownAuthors);
        ArgumentNullException.ThrowIfNull(localize);

        if (bookTitles.Count == 0)
        {
            throw new ArgumentException("At least one book title is required.", nameof(bookTitles));
        }

        AffectedBookCount = bookTitles.Count;
        BookTitle = AffectedBookCount == 1 ? bookTitles.Single() : null;
        ContextText = BookTitle ?? string.Format(
            CultureInfo.CurrentCulture,
            localize("MetadataQualityAuthorRepairBulkBookContext"),
            AffectedBookCount);
        SaveButtonText = AffectedBookCount == 1
            ? localize("MetadataQualityAuthorRepairSave")
            : string.Format(
                CultureInfo.CurrentCulture,
                localize("MetadataQualityAuthorRepairBulkSave"),
                AffectedBookCount);
        this.knownAuthors = knownAuthors
            .Where(MetadataQualityAuthorRules.IsUsable)
            .Select(author => author.Trim())
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(author => author, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        suggestions = this.knownAuthors;
    }

    public int AffectedBookCount { get; }
    public string? BookTitle { get; }
    public string ContextText { get; }
    public string SaveButtonText { get; }
    public string? NormalizedAuthor => MetadataQualityAuthorRules.IsUsable(AuthorText) ? AuthorText.Trim() : null;
    public bool CanSave => NormalizedAuthor is not null;

    public void UseSuggestion(string? author)
    {
        if (!string.IsNullOrWhiteSpace(author))
        {
            AuthorText = author;
        }
    }

    partial void OnAuthorTextChanged(string value) =>
        Suggestions = FilterSuggestions(value);

    private IReadOnlyList<string> FilterSuggestions(string value)
    {
        var query = value.Trim();
        if (query.Length == 0)
        {
            return knownAuthors;
        }

        return knownAuthors
            .Where(author => author.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(author => author.StartsWith(query, StringComparison.CurrentCultureIgnoreCase) ? 0 : 1)
            .ThenBy(author => author, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }
}

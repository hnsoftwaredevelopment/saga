using CommunityToolkit.Mvvm.ComponentModel;

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
    {
        ArgumentNullException.ThrowIfNull(knownAuthors);

        BookTitle = bookTitle;
        this.knownAuthors = knownAuthors
            .Where(IsUsableAuthor)
            .Select(author => author.Trim())
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(author => author, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        suggestions = this.knownAuthors;
    }

    public string BookTitle { get; }
    public string? NormalizedAuthor => IsUsableAuthor(AuthorText) ? AuthorText.Trim() : null;
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

    private static bool IsUsableAuthor(string? author) =>
        !string.IsNullOrWhiteSpace(author) &&
        !author.Trim().Equals("Unknown", StringComparison.OrdinalIgnoreCase);
}

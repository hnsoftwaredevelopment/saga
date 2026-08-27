using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class MetadataQualityDashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private MetadataQualityIssueViewModel? selectedIssue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedBookId))]
    private MetadataQualityBookRowViewModel? selectedBook;

    public MetadataQualityDashboardViewModel(
        IReadOnlyList<Book> books,
        Func<string, string> localize)
    {
        TotalBookCount = books.Count;
        Issues = new ObservableCollection<MetadataQualityIssueViewModel>(
            BuildIssues(books, localize));
        SelectedIssue = Issues.FirstOrDefault(issue => issue.Count > 0) ?? Issues.FirstOrDefault();
    }

    public int TotalBookCount { get; }
    public ObservableCollection<MetadataQualityIssueViewModel> Issues { get; }
    public bool HasIssues => Issues.Any(issue => issue.Count > 0);
    public int TotalIssueCount => Issues.Sum(issue => issue.Count);
    public Guid? SelectedBookId => SelectedBook?.Id;

    partial void OnSelectedIssueChanged(MetadataQualityIssueViewModel? value) =>
        SelectedBook = value?.Rows.FirstOrDefault();

    private static IReadOnlyList<MetadataQualityIssueViewModel> BuildIssues(
        IReadOnlyList<Book> books,
        Func<string, string> localize) =>
        [
            CreateIssue(
                localize("MetadataQualityMissingAuthor"),
                localize("MetadataQualityMissingAuthorDescription"),
                books.Where(HasMissingAuthor)),
            CreateIssue(
                localize("MetadataQualityUnknownLanguage"),
                localize("MetadataQualityUnknownLanguageDescription"),
                books.Where(HasUnknownLanguage)),
            CreateIssue(
                localize("MetadataQualityMissingCover"),
                localize("MetadataQualityMissingCoverDescription"),
                books.Where(HasMissingCover)),
            CreateIssue(
                localize("MetadataQualitySeriesNumberWithoutSeries"),
                localize("MetadataQualitySeriesNumberWithoutSeriesDescription"),
                books.Where(HasSeriesNumberWithoutSeries)),
            CreateIssue(
                localize("MetadataQualityPossibleTitleAuthorSwap"),
                localize("MetadataQualityPossibleTitleAuthorSwapDescription"),
                books.Where(HasPossibleTitleAuthorSwap)),
            CreateIssue(
                localize("MetadataQualityMessyTags"),
                localize("MetadataQualityMessyTagsDescription"),
                books.Where(HasMessyTags))
        ];

    private static MetadataQualityIssueViewModel CreateIssue(
        string title,
        string description,
        IEnumerable<Book> books)
    {
        var rows = books
            .OrderBy(book => book.Metadata.Title, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(book => string.Join(", ", book.Metadata.Authors), StringComparer.CurrentCultureIgnoreCase)
            .Select(book => new MetadataQualityBookRowViewModel(book))
            .ToArray();
        return new MetadataQualityIssueViewModel(title, description, rows);
    }

    private static bool HasMissingAuthor(Book book) =>
        book.Metadata.Authors.Count == 0 ||
        book.Metadata.Authors.All(author =>
            string.IsNullOrWhiteSpace(author) ||
            author.Equals("Unknown", StringComparison.OrdinalIgnoreCase));

    private static bool HasUnknownLanguage(Book book)
    {
        var language = book.Metadata.Language;
        if (string.IsNullOrWhiteSpace(language))
        {
            return true;
        }

        var key = LanguageDisplayService.FilterKey(language);
        if (string.IsNullOrWhiteSpace(key))
        {
            return true;
        }

        try
        {
            _ = System.Globalization.CultureInfo.GetCultureInfo(key);
            return false;
        }
        catch (System.Globalization.CultureNotFoundException)
        {
            return true;
        }
    }

    private static bool HasMissingCover(Book book) =>
        book.Metadata.CoverBytes is null &&
        string.IsNullOrWhiteSpace(book.CoverRelativePath);

    private static bool HasSeriesNumberWithoutSeries(Book book) =>
        book.Metadata.SeriesNumber is not null &&
        string.IsNullOrWhiteSpace(book.Metadata.Series);

    private static bool HasPossibleTitleAuthorSwap(Book book)
    {
        if (book.Metadata.Authors.Count != 1)
        {
            return false;
        }

        var title = book.Metadata.Title.Trim();
        var author = book.Metadata.Authors[0].Trim();
        return LooksLikePersonName(title) && !LooksLikePersonName(author);
    }

    private static bool HasMessyTags(Book book)
    {
        var tags = book.Metadata.Tags ?? [];
        return tags.Any(tag =>
            string.IsNullOrWhiteSpace(tag) ||
            tag != tag.Trim() ||
            tag.Contains("  ", StringComparison.Ordinal) ||
            tag.Contains(',', StringComparison.Ordinal));
    }

    private static bool LooksLikePersonName(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length is >= 2 and <= 4 &&
            parts.All(part => part.Length > 1 && char.IsUpper(part[0]) && part.Skip(1).Any(char.IsLower));
    }
}

public sealed class MetadataQualityIssueViewModel(
    string title,
    string description,
    IReadOnlyList<MetadataQualityBookRowViewModel> rows)
{
    public string Title { get; } = title;
    public string Description { get; } = description;
    public int Count => Rows.Count;
    public IReadOnlyList<MetadataQualityBookRowViewModel> Rows { get; } = rows;
}

public sealed class MetadataQualityBookRowViewModel(Book book)
{
    public Guid Id { get; } = book.Id;
    public string Title { get; } = book.Metadata.Title;
    public string Authors { get; } = string.Join(", ", book.Metadata.Authors);
    public string Series { get; } = book.Metadata.Series ?? string.Empty;
    public string Language { get; } = book.Metadata.Language ?? string.Empty;
    public string Tags { get; } = string.Join("; ", book.Metadata.Tags ?? []);
}

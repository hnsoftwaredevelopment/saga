using EbookManager.Domain.Books;
using EbookManager.Application.Metadata;
using System.Globalization;

namespace EbookManager.Application.Books;

public sealed class BookSearchService
{
    public IReadOnlyList<Book> Filter(IReadOnlyList<Book> books, string? searchText)
    {
        ArgumentNullException.ThrowIfNull(books);

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return books;
        }

        var normalizedSearchText = searchText.Trim();
        return books.Where(book => Matches(book, normalizedSearchText)).ToList();
    }

    private static bool Matches(Book book, string searchText) =>
        Contains(book.Metadata.Title, searchText) ||
        book.Metadata.Authors.Any(author => Contains(author, searchText)) ||
        Contains(book.Metadata.Description, searchText) ||
        Contains(book.Metadata.Language, searchText) ||
        Contains(LanguageDisplayName(book.Metadata.Language), searchText) ||
        Contains(book.Metadata.Publisher, searchText) ||
        MatchesDate(book.Metadata.PublicationDate, searchText) ||
        (book.Metadata.Tags?.Any(tag => Contains(tag, searchText)) ?? false) ||
        Contains(book.Metadata.Series, searchText) ||
        MatchesNumber(book.Metadata.SeriesNumber, searchText) ||
        Contains(book.Metadata.Isbn, searchText) ||
        book.Formats.Any(format => Contains(format.ToString(), searchText)) ||
        MatchesDateTime(book.CreatedUtc, searchText) ||
        MatchesDateTime(book.UpdatedUtc, searchText) ||
        MatchesReadingStatus(book.ReadingStatus, searchText);

    private static bool Contains(string? value, string searchText) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private static string? LanguageDisplayName(string? language) =>
        string.IsNullOrWhiteSpace(language)
            ? null
            : LanguageDisplayService.DisplayName(language);

    private static bool MatchesDate(DateOnly? value, string searchText) =>
        value is not null &&
        (Contains(value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), searchText) ||
         Contains(value.Value.ToString("d", CultureInfo.CurrentCulture), searchText));

    private static bool MatchesDateTime(DateTimeOffset value, string searchText)
    {
        var local = value.ToLocalTime();
        return Contains(local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), searchText) ||
            Contains(local.ToString("g", CultureInfo.CurrentCulture), searchText);
    }

    private static bool MatchesNumber(decimal? value, string searchText) =>
        value is not null &&
        (Contains(value.Value.ToString(CultureInfo.InvariantCulture), searchText) ||
         Contains(value.Value.ToString(CultureInfo.CurrentCulture), searchText));

    private static bool MatchesReadingStatus(ReadingStatus status, string searchText) =>
        status.ToString().Equals(searchText, StringComparison.OrdinalIgnoreCase);
}

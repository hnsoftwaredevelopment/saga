using EbookManager.Domain.Books;
using EbookManager.Application.Metadata;
using System.Globalization;

namespace EbookManager.Application.Books;

public sealed class BookSearchService
{
    public IReadOnlyList<Book> Filter(IReadOnlyList<Book> books, string? searchText)
    {
        return Filter(books, searchText, null);
    }

    public IReadOnlyList<Book> Filter(
        IReadOnlyList<Book> books,
        string? searchText,
        Func<Book, IEnumerable<string?>>? extraValuesSelector)
    {
        ArgumentNullException.ThrowIfNull(books);

        return Filter(CreateIndex(books, extraValuesSelector), searchText);
    }

    public BookSearchIndex CreateIndex(
        IReadOnlyList<Book> books,
        Func<Book, IEnumerable<string?>>? extraValuesSelector = null)
    {
        ArgumentNullException.ThrowIfNull(books);

        return new BookSearchIndex(books.Select(book => CreateEntry(book, extraValuesSelector)).ToArray());
    }

    public IReadOnlyList<Book> Filter(BookSearchIndex index, string? searchText)
    {
        ArgumentNullException.ThrowIfNull(index);

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return index.Books;
        }

        var normalizedSearchText = searchText.Trim();
        return index.Entries
            .Where(entry => Matches(entry, normalizedSearchText))
            .Select(entry => entry.Book)
            .ToList();
    }

    private static BookSearchEntry CreateEntry(
        Book book,
        Func<Book, IEnumerable<string?>>? extraValuesSelector)
    {
        var values = new List<string?>
        {
            book.Metadata.Title,
            book.Metadata.Description,
            book.Metadata.Language,
            LanguageDisplayName(book.Metadata.Language),
            book.Metadata.Publisher,
            book.Metadata.Series,
            book.Metadata.Isbn
        };
        values.AddRange(book.Metadata.Authors);
        values.AddRange(book.Metadata.Tags ?? []);
        values.AddRange(book.Formats.Select(format => format.ToString()));
        AddDateValues(values, book.Metadata.PublicationDate);
        AddNumberValues(values, book.Metadata.SeriesNumber);
        AddDateTimeValues(values, book.CreatedUtc);
        AddDateTimeValues(values, book.UpdatedUtc);
        values.AddRange(extraValuesSelector?.Invoke(book) ?? []);

        return new BookSearchEntry(
            book,
            values.Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>().ToArray(),
            book.ReadingStatus.ToString());
    }

    private static bool Matches(BookSearchEntry entry, string searchText) =>
        entry.Values.Any(value => Contains(value, searchText)) ||
        entry.ReadingStatus.Equals(searchText, StringComparison.OrdinalIgnoreCase);

    private static bool Contains(string? value, string searchText) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private static string? LanguageDisplayName(string? language) =>
        string.IsNullOrWhiteSpace(language)
            ? null
            : LanguageDisplayService.DisplayName(language);

    private static void AddDateValues(ICollection<string?> values, DateOnly? value)
    {
        if (value is null)
        {
            return;
        }

        values.Add(value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        values.Add(value.Value.ToString("d", CultureInfo.CurrentCulture));
    }

    private static void AddDateTimeValues(ICollection<string?> values, DateTimeOffset value)
    {
        var local = value.ToLocalTime();
        values.Add(local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        values.Add(local.ToString("g", CultureInfo.CurrentCulture));
    }

    private static void AddNumberValues(ICollection<string?> values, decimal? value)
    {
        if (value is null)
        {
            return;
        }

        values.Add(value.Value.ToString(CultureInfo.InvariantCulture));
        values.Add(value.Value.ToString(CultureInfo.CurrentCulture));
    }
}

public sealed class BookSearchIndex
{
    internal BookSearchIndex(IReadOnlyList<BookSearchEntry> entries)
    {
        Entries = entries;
        Books = entries.Select(entry => entry.Book).ToArray();
    }

    internal IReadOnlyList<BookSearchEntry> Entries { get; }
    public IReadOnlyList<Book> Books { get; }
}

internal sealed record BookSearchEntry(
    Book Book,
    IReadOnlyList<string> Values,
    string ReadingStatus);

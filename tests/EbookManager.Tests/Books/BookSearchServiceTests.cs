using EbookManager.Application.Books;
using EbookManager.Domain.Books;
using FluentAssertions;
using System.Globalization;

namespace EbookManager.Tests.Books;

public sealed class BookSearchServiceTests
{
    [Theory]
    [InlineData("HOBBIT", "The Hobbit")]
    [InlineData("TOLKIEN", "The Silmarillion")]
    [InlineData("DRAGON", "Dragon Tales")]
    [InlineData("DUTCH", "Nederlands Boek")]
    [InlineData("PENGUIN", "Publisher Match")]
    [InlineData("SPACE", "Tagged Book")]
    [InlineData("FOUNDATION", "Series Match")]
    [InlineData("1.5", "Series Number Match")]
    [InlineData("9780000000000", "ISBN Match")]
    [InlineData("EPUB", "Format Match")]
    [InlineData("2020-01-02", "Publication Date Match")]
    [InlineData("2026-07-15", "Created Date Match")]
    [InlineData("2026-07-16", "Updated Date Match")]
    [InlineData("READ", "Status Book")]
    public void Filter_matches_each_supported_field_case_insensitively(string searchText, string expectedTitle)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var service = new BookSearchService();
            var books = CreateBooks();

            var result = service.Filter(books, searchText);

            result.Should().ContainSingle(book => book.Metadata.Title == expectedTitle);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void Filter_matches_localized_language_display_name()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("nl-NL");
            var service = new BookSearchService();
            var books = CreateBooks();

            var result = service.Filter(books, "Nederlands");

            result.Should().ContainSingle(book => book.Metadata.Title == "Nederlands Boek");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_search_returns_all_books(string searchText)
    {
        var service = new BookSearchService();
        var books = CreateBooks();

        var result = service.Filter(books, searchText);

        result.Should().HaveCount(books.Count);
    }

    [Fact]
    public void Reading_status_search_matches_the_exact_status_name()
    {
        var service = new BookSearchService();
        var books = CreateBooks();

        var result = service.Filter(books, "read");

        result.Should().ContainSingle(book => book.ReadingStatus == ReadingStatus.Read);
    }

    private static IReadOnlyList<Book> CreateBooks()
    {
        var now = new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero);
        return
        [
            new(Guid.NewGuid(), new BookMetadata("The Hobbit", ["Bilbo Baggins"]), ReadingStatus.Unread, null, now, now),
            new(Guid.NewGuid(), new BookMetadata("The Silmarillion", ["Tolkien"]), ReadingStatus.Unread, null, now, now),
            new(Guid.NewGuid(), new BookMetadata("Dragon Tales", ["Author"], Description: "A dragon adventure"), ReadingStatus.Unread, null, now, now),
            new(Guid.NewGuid(), new BookMetadata("Nederlands Boek", ["Auteur"], Language: "nl"), ReadingStatus.Unread, null, now, now),
            new(Guid.NewGuid(), new BookMetadata("Publisher Match", ["Author"], Publisher: "Penguin House"), ReadingStatus.Unread, null, now, now),
            new(Guid.NewGuid(), new BookMetadata("Tagged Book", ["Author"], Tags: ["Space", "History"]), ReadingStatus.Unread, null, now, now),
            new(Guid.NewGuid(), new BookMetadata("Series Match", ["Author"], Series: "Foundation"), ReadingStatus.Unread, null, now, now),
            new(Guid.NewGuid(), new BookMetadata("Series Number Match", ["Author"], SeriesNumber: 1.5m), ReadingStatus.Unread, null, now, now),
            new(Guid.NewGuid(), new BookMetadata("ISBN Match", ["Author"], Isbn: "9780000000000"), ReadingStatus.Unread, null, now, now),
            new(Guid.NewGuid(), new BookMetadata("Format Match", ["Author"]), ReadingStatus.Unread, null, now, now)
            {
                Formats = [EbookFormat.Epub]
            },
            new(Guid.NewGuid(), new BookMetadata("Publication Date Match", ["Author"], PublicationDate: new DateOnly(2020, 1, 2)), ReadingStatus.Unread, null, now, now),
            new(
                Guid.NewGuid(),
                new BookMetadata("Created Date Match", ["Author"]),
                ReadingStatus.Unread,
                null,
                new DateTimeOffset(2026, 7, 15, 10, 30, 0, TimeSpan.Zero),
                now),
            new(
                Guid.NewGuid(),
                new BookMetadata("Updated Date Match", ["Author"]),
                ReadingStatus.Unread,
                null,
                now,
                new DateTimeOffset(2026, 7, 16, 11, 45, 0, TimeSpan.Zero)),
            new(Guid.NewGuid(), new BookMetadata("Status Book", ["Author"]), ReadingStatus.Read, null, now, now)
        ];
    }
}

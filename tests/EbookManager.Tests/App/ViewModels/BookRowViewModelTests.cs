using EbookManager.Domain.Books;
using EbookManager.Domain.Settings;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class BookRowViewModelTests
{
    [Fact]
    public void SeriesNumber_remains_numeric_for_grid_sorting()
    {
        var row = new BookRowViewModel(CreateBook(10));

        row.SeriesNumber.Should().Be(10);
        row.SeriesNumberText.Should().Be(10m.ToString(System.Globalization.CultureInfo.CurrentCulture));
    }

    [Fact]
    public void AuthorsSortKey_uses_configured_author_sort_strategy()
    {
        var row = new BookRowViewModel(CreateBook(1, "Karin Slaughter"), authorSortStrategy: AuthorSortStrategy.LastNameFirst);

        row.Authors.Should().Be("Karin Slaughter");
        row.AuthorsSortKey.Should().Be("Slaughter, Karin");
    }

    [Fact]
    public void Date_sort_values_remain_typed_while_display_values_are_localized_text()
    {
        var created = new DateTimeOffset(2026, 7, 24, 10, 30, 0, TimeSpan.Zero);
        var updated = new DateTimeOffset(2026, 7, 25, 11, 45, 0, TimeSpan.Zero);
        var book = new Book(
            Guid.NewGuid(),
            new BookMetadata("Title", ["Author"], PublicationDate: new DateOnly(2025, 12, 31)),
            ReadingStatus.Unread,
            null,
            created,
            updated);

        var row = new BookRowViewModel(book);

        row.PublicationDateSortValue.Should().Be(new DateOnly(2025, 12, 31));
        row.DateAddedSortValue.Should().Be(created);
        row.LastModifiedSortValue.Should().Be(updated);
        row.PublicationDate.Should().NotBeNullOrWhiteSpace();
        row.DateAdded.Should().NotBeNullOrWhiteSpace();
        row.LastModified.Should().NotBeNullOrWhiteSpace();
    }

    private static Book CreateBook(decimal seriesNumber, string author = "Author")
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata("Title", [author], Series: "Series", SeriesNumber: seriesNumber),
            ReadingStatus.Unread,
            null,
            now,
            now);
    }
}

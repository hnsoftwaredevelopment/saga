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

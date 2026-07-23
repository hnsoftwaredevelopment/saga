using EbookManager.Domain.Books;
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

    private static Book CreateBook(decimal seriesNumber)
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata("Title", ["Author"], Series: "Series", SeriesNumber: seriesNumber),
            ReadingStatus.Unread,
            null,
            now,
            now);
    }
}

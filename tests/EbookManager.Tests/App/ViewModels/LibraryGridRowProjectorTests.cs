using EbookManager.Domain.Books;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class LibraryGridRowProjectorTests
{
    [Fact]
    public void Project_splits_grouped_authors_without_changing_display_text()
    {
        var book = CreateBook(
            "Shared Story",
            ["Jan Wiersma", "Sonja de Leeuw"],
            series: "Samen");
        var row = new BookRowViewModel(book);

        var projected = LibraryGridRowProjector.Project(
            [row],
            [nameof(BookRowViewModel.AuthorsGroupKey), nameof(BookRowViewModel.Series)]);

        projected.Should().HaveCount(2);
        projected.Select(item => item.AuthorsGroupKey)
            .Should().BeEquivalentTo("Jan Wiersma", "Sonja de Leeuw");
        projected.Should().OnlyContain(item => item.Authors == "Jan Wiersma, Sonja de Leeuw");
        projected.Should().OnlyContain(item => item.Series == "Samen");
    }

    [Fact]
    public void Project_creates_combinations_for_multiple_multi_value_group_columns()
    {
        var book = CreateBook(
            "Tagged Story",
            ["Author"],
            tags: ["History", "Space"],
            formats: [EbookFormat.Epub, EbookFormat.Pdf]);
        var row = new BookRowViewModel(book);

        var projected = LibraryGridRowProjector.Project(
            [row],
            [nameof(BookRowViewModel.TagsGroupKey), nameof(BookRowViewModel.FormatsGroupKey)]);

        projected.Should().HaveCount(4);
        projected.Select(item => $"{item.TagsGroupKey}|{item.FormatsGroupKey}")
            .Should().BeEquivalentTo(
                "History|EPUB",
                "History|PDF",
                "Space|EPUB",
                "Space|PDF");
        projected.Should().OnlyContain(item => item.Tags == "History, Space");
        projected.Should().OnlyContain(item => item.Formats == "EPUB, PDF");
    }

    [Fact]
    public void Project_keeps_rows_unique_when_no_multi_value_grouping_is_active()
    {
        var row = new BookRowViewModel(CreateBook("Solo", ["Author"]));

        var projected = LibraryGridRowProjector.Project(
            [row],
            [nameof(BookRowViewModel.Series)]);

        projected.Should().ContainSingle().Which.Should().BeSameAs(row);
    }

    private static Book CreateBook(
        string title,
        IReadOnlyList<string> authors,
        string? series = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyList<EbookFormat>? formats = null) =>
        new(
            Guid.NewGuid(),
            new BookMetadata(
                title,
                authors,
                Tags: tags,
                Series: series),
            ReadingStatus.Unread,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow)
        {
            Formats = formats ?? [EbookFormat.Epub]
        };
}

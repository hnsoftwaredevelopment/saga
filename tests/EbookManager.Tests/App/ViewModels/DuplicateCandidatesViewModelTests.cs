using EbookManager.Application.Books;
using EbookManager.Domain.Books;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class DuplicateCandidatesViewModelTests
{
    [Fact]
    public void Rows_flatten_groups_for_grid_display()
    {
        var first = CreateBook(
            "De Hobbit",
            ["J.R.R. Tolkien"],
            series: "Midden-aarde",
            language: "nl",
            description: "Een reis door Midden-aarde.",
            coverRelativePath: "books/cover.jpg");
        var second = CreateBook("de hobbit", ["J.R.R. Tolkien"], series: "Middle-earth", language: "eng");
        var result = new DuplicateCandidateResult(
        [
            new DuplicateCandidateGroup("de hobbit:0", "De Hobbit", "J.R.R. Tolkien", [first, second])
        ]);

        var viewModel = new DuplicateCandidatesViewModel(result, "C:/Library");

        viewModel.Rows.Should().HaveCount(2);
        viewModel.Rows[0].GroupTitle.Should().Be("De Hobbit - J.R.R. Tolkien");
        viewModel.Rows[0].Title.Should().Be("De Hobbit");
        viewModel.Rows[0].Authors.Should().Be("J.R.R. Tolkien");
        viewModel.Rows[0].Series.Should().Be("Midden-aarde");
        viewModel.Rows[0].Language.Should().Be("nl");
        viewModel.Rows[0].Description.Should().Be("Een reis door Midden-aarde.");
        viewModel.Rows[0].CoverPath.Should().Be(Path.Combine("C:/Library", "books/cover.jpg"));
    }

    private static Book CreateBook(
        string title,
        IReadOnlyList<string> authors,
        string? series,
        string? language,
        string? description = null,
        string? coverRelativePath = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata(title, authors, Description: description, Language: language, Series: series),
            ReadingStatus.Unread,
            coverRelativePath,
            now,
            now);
    }
}

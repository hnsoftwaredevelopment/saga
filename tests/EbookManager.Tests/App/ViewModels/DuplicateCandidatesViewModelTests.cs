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
        var first = CreateBook("De Hobbit", ["J.R.R. Tolkien"], series: "Midden-aarde", language: "nl");
        var second = CreateBook("de hobbit", ["J.R.R. Tolkien"], series: "Middle-earth", language: "eng");
        var result = new DuplicateCandidateResult(
        [
            new DuplicateCandidateGroup("de hobbit:0", "De Hobbit", "J.R.R. Tolkien", [first, second])
        ]);

        var viewModel = new DuplicateCandidatesViewModel(result);

        viewModel.Rows.Should().HaveCount(2);
        viewModel.Rows[0].GroupTitle.Should().Be("De Hobbit - J.R.R. Tolkien");
        viewModel.Rows[0].Title.Should().Be("De Hobbit");
        viewModel.Rows[0].Authors.Should().Be("J.R.R. Tolkien");
        viewModel.Rows[0].Series.Should().Be("Midden-aarde");
        viewModel.Rows[0].Language.Should().Be("nl");
    }

    private static Book CreateBook(
        string title,
        IReadOnlyList<string> authors,
        string? series,
        string? language)
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata(title, authors, Language: language, Series: series),
            ReadingStatus.Unread,
            CoverRelativePath: null,
            now,
            now);
    }
}

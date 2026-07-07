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
        viewModel.Rows[0].FormatText.Should().Be("EPUB, PDF");
        viewModel.Rows[0].MatchKind.Should().Be(DuplicateCandidateMatchKind.AuthorOverlap);
        viewModel.Rows[0].Description.Should().Be("Een reis door Midden-aarde.");
        viewModel.Rows[0].CoverPath.Should().Be(Path.Combine("C:/Library", "books/cover.jpg"));
    }

    [Fact]
    public void Rows_expose_title_only_match_kind_for_display()
    {
        var first = CreateBook("De Chocoladevilla", ["Maria Nikolai"], series: null, language: "nl");
        var second = CreateBook("De chocoladevilla", ["Unknown"], series: null, language: null);
        var result = new DuplicateCandidateResult(
        [
            new DuplicateCandidateGroup(
                "de chocoladevilla:title",
                "De Chocoladevilla",
                "Maria Nikolai, Unknown",
                [first, second],
                DuplicateCandidateMatchKind.TitleOnly)
        ]);

        var viewModel = new DuplicateCandidatesViewModel(result, "C:/Library");

        viewModel.Rows.Should().HaveCount(2);
        viewModel.Rows.Should().OnlyContain(row => row.MatchKind == DuplicateCandidateMatchKind.TitleOnly);
    }

    [Fact]
    public async Task DeleteCandidate_removes_book_and_recomputes_duplicate_groups()
    {
        var first = CreateBook("De Hobbit", ["J.R.R. Tolkien"], series: null, language: null);
        var second = CreateBook("de hobbit", ["J.R.R. Tolkien"], series: null, language: null);
        var third = CreateBook("de hobbit", ["J.R.R. Tolkien"], series: null, language: null);
        var result = new DuplicateCandidateService().FindCandidates([first, second, third]);
        var deletedIds = new List<Guid>();
        var viewModel = new DuplicateCandidatesViewModel(
            result,
            deleteCandidateAsync: (row, _) =>
            {
                deletedIds.Add(row.Id);
                return Task.FromResult(true);
            });

        await viewModel.DeleteCandidateAsync(viewModel.Rows[0], CancellationToken.None);

        deletedIds.Should().Equal(first.Id);
        viewModel.HasChanges.Should().BeTrue();
        viewModel.GroupCount.Should().Be(1);
        viewModel.BookCount.Should().Be(2);
        viewModel.Rows.Select(row => row.Id).Should().BeEquivalentTo([second.Id, third.Id]);
    }

    [Fact]
    public async Task DeleteCandidate_closes_duplicate_list_when_no_duplicate_groups_remain()
    {
        var first = CreateBook("De Hobbit", ["J.R.R. Tolkien"], series: null, language: null);
        var second = CreateBook("de hobbit", ["J.R.R. Tolkien"], series: null, language: null);
        var result = new DuplicateCandidateService().FindCandidates([first, second]);
        var viewModel = new DuplicateCandidatesViewModel(
            result,
            deleteCandidateAsync: (_, _) => Task.FromResult(true));

        await viewModel.DeleteCandidateAsync(viewModel.Rows[0], CancellationToken.None);

        viewModel.HasChanges.Should().BeTrue();
        viewModel.HasGroups.Should().BeFalse();
        viewModel.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteSelectedCandidates_deletes_selected_rows_and_recomputes_duplicate_groups()
    {
        var first = CreateBook("De Hobbit", ["J.R.R. Tolkien"], series: null, language: null);
        var second = CreateBook("de hobbit", ["J.R.R. Tolkien"], series: null, language: null);
        var third = CreateBook("de hobbit", ["J.R.R. Tolkien"], series: null, language: null);
        var result = new DuplicateCandidateService().FindCandidates([first, second, third]);
        var deletedIds = new List<Guid>();
        var viewModel = new DuplicateCandidatesViewModel(
            result,
            deleteCandidateAsync: (row, _) =>
            {
                deletedIds.Add(row.Id);
                return Task.FromResult(true);
            });
        var selectedIds = viewModel.Rows.Take(2).Select(row => row.Id).ToList();
        viewModel.Rows[0].IsSelected = true;
        viewModel.Rows[1].IsSelected = true;

        await viewModel.DeleteSelectedCandidatesCommand.ExecuteAsync(null);

        deletedIds.Should().Equal(selectedIds);
        viewModel.HasChanges.Should().BeTrue();
        viewModel.HasGroups.Should().BeFalse();
        viewModel.Rows.Should().BeEmpty();
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
            now)
        {
            Formats = [EbookFormat.Epub, EbookFormat.Pdf]
        };
    }
}

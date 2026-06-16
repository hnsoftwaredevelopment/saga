using EbookManager.Application.Books;
using EbookManager.Domain.Books;
using FluentAssertions;

namespace EbookManager.Tests.Books;

public sealed class DuplicateCandidateServiceTests
{
    [Fact]
    public void FindCandidates_groups_books_with_the_same_normalized_title()
    {
        var service = new DuplicateCandidateService();
        var first = CreateBook("De Hobbit", ["J.R.R. Tolkien"]);
        var second = CreateBook(" de hobbit ", ["Tolkien, J.R.R."]);
        var unrelated = CreateBook("Dune", ["Frank Herbert"]);

        var result = service.FindCandidates([first, second, unrelated]);

        result.Groups.Should().ContainSingle();
        var group = result.Groups[0];
        group.DisplayTitle.Should().Be("De Hobbit");
        group.Books.Should().Equal(first, second);
    }

    [Fact]
    public void FindCandidates_ignores_titles_that_only_occur_once()
    {
        var service = new DuplicateCandidateService();
        var first = CreateBook("De Hobbit", ["J.R.R. Tolkien"]);
        var second = CreateBook("Dune", ["Frank Herbert"]);

        var result = service.FindCandidates([first, second]);

        result.Groups.Should().BeEmpty();
    }

    private static Book CreateBook(string title, IReadOnlyList<string> authors) =>
        new(
            Guid.NewGuid(),
            new BookMetadata(title, authors),
            ReadingStatus.Unread,
            CoverRelativePath: null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
}

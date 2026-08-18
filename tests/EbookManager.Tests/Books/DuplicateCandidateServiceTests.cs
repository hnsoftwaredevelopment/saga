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
        var second = CreateBook(" de hobbit ", ["J.R.R. Tolkien", "Alan Lee"]);
        var unrelated = CreateBook("Dune", ["Frank Herbert"]);

        var result = service.FindCandidates([first, second, unrelated]);

        result.Groups.Should().ContainSingle();
        var group = result.Groups[0];
        group.DisplayTitle.Should().Be("De Hobbit");
        group.Books.Should().Equal(first, second);
    }

    [Fact]
    public void FindCandidates_marks_same_title_without_author_overlap_as_title_only_match()
    {
        var service = new DuplicateCandidateService();
        var first = CreateBook("De Hobbit", ["J.R.R. Tolkien"]);
        var second = CreateBook("de hobbit", ["John Ronald Reuel Tolkien"]);

        var result = service.FindCandidates([first, second]);

        result.Groups.Should().ContainSingle();
        result.Groups[0].MatchKind.Should().Be(DuplicateCandidateMatchKind.TitleOnly);
        result.Groups[0].Books.Should().Equal(first, second);
    }

    [Fact]
    public void FindCandidates_splits_same_title_groups_when_authors_do_not_overlap()
    {
        var service = new DuplicateCandidateService();
        var firstAuthorFirst = CreateBook("De Stad", ["Author One"]);
        var firstAuthorSecond = CreateBook("de stad", ["Author One"]);
        var secondAuthorFirst = CreateBook("De Stad", ["Author Two"]);
        var secondAuthorSecond = CreateBook("de stad", ["Author Two"]);

        var result = service.FindCandidates([firstAuthorFirst, firstAuthorSecond, secondAuthorFirst, secondAuthorSecond]);

        result.Groups.Should().HaveCount(2);
        result.Groups.Should().Contain(group => group.Books.Select(book => book.Id).SequenceEqual(
            new[] { firstAuthorFirst.Id, firstAuthorSecond.Id }));
        result.Groups.Should().Contain(group => group.Books.Select(book => book.Id).SequenceEqual(
            new[] { secondAuthorFirst.Id, secondAuthorSecond.Id }));
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

    [Fact]
    public void FindCandidates_ignores_excluded_author_overlap_pairs()
    {
        var service = new DuplicateCandidateService();
        var first = CreateBook("De Hobbit", ["J.R.R. Tolkien"]);
        var second = CreateBook("de hobbit", ["J.R.R. Tolkien"]);
        var exclusions = new HashSet<DuplicateExclusionPair>
        {
            DuplicateExclusionPair.Create(first.Id, second.Id)
        };

        var result = service.FindCandidates([first, second], exclusions);

        result.Groups.Should().BeEmpty();
    }

    [Fact]
    public void FindCandidates_keeps_nonexcluded_books_in_large_title_group()
    {
        var service = new DuplicateCandidateService();
        var first = CreateBook("De Stad", ["Author One"]);
        var second = CreateBook("De Stad", ["Author Two"]);
        var third = CreateBook("De Stad", ["Author Three"]);
        var exclusions = new HashSet<DuplicateExclusionPair>
        {
            DuplicateExclusionPair.Create(first.Id, second.Id)
        };

        var result = service.FindCandidates([first, second, third], exclusions);

        result.Groups.Should().ContainSingle();
        result.Groups[0].MatchKind.Should().Be(DuplicateCandidateMatchKind.TitleOnly);
        result.Groups[0].Books.Select(book => book.Id).Should().BeEquivalentTo([first.Id, third.Id, second.Id]);
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

using EbookManager.Application.Authors;
using EbookManager.Domain.Authors;
using FluentAssertions;

namespace EbookManager.Tests.Authors;

public sealed class AuthorDuplicateFinderTests
{
    [Theory]
    [InlineData("J. Nowee", "J.Nowee", AuthorDuplicateReason.NameFormatting)]
    [InlineData("Nowee, J.", "J. Nowee", AuthorDuplicateReason.NameFormatting)]
    [InlineData("J.Nowee", "Johannes Nowee", AuthorDuplicateReason.MatchingInitials)]
    [InlineData("A.K. Straatsma", "Anne Klaas Straatsma", AuthorDuplicateReason.MatchingInitials)]
    [InlineData("J. van der Spek", "van der Spek, Johannes", AuthorDuplicateReason.MatchingInitials)]
    [InlineData("A.L.G. Bosboom-Toussaint", "Bosboom-Toussaint, A. L. G.", AuthorDuplicateReason.NameFormatting)]
    [InlineData("José García", "Jose Garcia", AuthorDuplicateReason.NameFormatting)]
    public void Finds_conservative_name_variants(string first, string second, AuthorDuplicateReason reason)
    {
        var result = AuthorDuplicateFinder.Find([Author(first), Author(second)]);
        result.Candidates.Should().ContainSingle().Which.Reason.Should().Be(reason);
    }

    [Theory]
    [InlineData("Jan Nowee", "Johannes Nowee")]
    [InlineData("J. Nowee", "P. Nowee")]
    [InlineData("John Smith", "Jane Smith")]
    [InlineData("J. van Loon", "J. Loon")]
    [InlineData("Nowee", "Johannes Nowee")]
    [InlineData("Unknown", "Unknown")]
    [InlineData("Author 1", "Author 2")]
    [InlineData("J. Nowee", "j@nowee")]
    public void Does_not_suggest_a_shared_surname_or_different_full_names(string first, string second)
        => AuthorDuplicateFinder.Find([Author(first), Author(second)]).Candidates.Should().BeEmpty();

    [Fact]
    public void Filters_both_names_limits_pairs_and_never_forms_transitive_merge_groups()
    {
        var authors = new[] { Author("J. Nowee"), Author("Johannes Nowee"), Author("Jan Nowee"), Author("A. Other"), Author("Anne Other") };
        AuthorDuplicateFinder.Find(authors, "Johannes").Candidates.Should().ContainSingle();
        AuthorDuplicateFinder.Find(authors, "absent").Candidates.Should().BeEmpty();
        var all = AuthorDuplicateFinder.Find(authors);
        all.Candidates.Should().HaveCount(3);
        all.Candidates.Should().NotContain(pair => pair.First.Name == "Jan Nowee" && pair.Second.Name == "Johannes Nowee");
        AuthorDuplicateFinder.Find(authors, limit: 1).Truncated.Should().BeTrue();
        AuthorDuplicateFinder.Find(authors, "Other", limit: 1).Truncated.Should().BeFalse();
        AuthorDuplicateFinder.Find([authors[0], authors[0]]).Candidates.Should().BeEmpty();
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        FluentActions.Invoking(() => AuthorDuplicateFinder.Find(authors, token: cancelled.Token)).Should().Throw<OperationCanceledException>();
    }
    private static AuthorSummary Author(string name) => new(Guid.NewGuid(), name, 1);
}

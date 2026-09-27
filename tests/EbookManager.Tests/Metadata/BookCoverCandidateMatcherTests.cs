using EbookManager.Application.Metadata;
using FluentAssertions;

namespace EbookManager.Tests.Metadata;

public sealed class BookCoverCandidateMatcherTests
{
    [Fact]
    public void Score_rejects_the_unrelated_results_seen_in_the_practical_test()
    {
        var query = new BookCoverSearchQuery("Ademloos - Sssst.... Luister", ["Huub Hovens"], null);

        Score(query, "Brinkman's cumulatieve catalogus", ["Redactie"]).Should().Be(0);
        Score(query, "Boekblad", []).Should().Be(0);
    }

    [Fact]
    public void Score_accepts_minor_title_punctuation_differences_and_the_same_author()
    {
        var query = new BookCoverSearchQuery("Ademloos - Sssst.... Luister", ["Huub Hovens"], null);

        Score(query, "Ademloos: sssst... luister!", ["Hovens, Huub"]).Should().BeGreaterThan(0);
    }

    [Fact]
    public void Score_requires_an_exact_title_when_the_provider_has_no_author()
    {
        var query = new BookCoverSearchQuery("De ontdekking van de hemel", ["Harry Mulisch"], null);

        Score(query, "De ontdekking van de hemel", []).Should().BeGreaterThan(0);
        Score(query, "Ontdekking van de hemel", []).Should().Be(0);
    }

    [Fact]
    public void Score_rejects_an_explicitly_different_author_even_when_the_title_matches()
    {
        var query = new BookCoverSearchQuery("De aanslag", ["Harry Mulisch"], null);

        Score(query, "De aanslag", ["Willem Frederik Hermans"]).Should().Be(0);
    }

    [Fact]
    public void Score_prefers_an_exact_valid_isbn()
    {
        var query = new BookCoverSearchQuery("Verkeerde titel", ["Onbekend"], "978-90-263-5660-5");
        var candidate = Candidate("Andere titel", ["Andere auteur"], ["9789026356605"]);

        BookCoverCandidateMatcher.Score(query, candidate).Should().BeGreaterThan(900);
    }

    private static int Score(BookCoverSearchQuery query, string title, IReadOnlyList<string> authors) =>
        BookCoverCandidateMatcher.Score(query, Candidate(title, authors));

    private static BookCoverCandidate Candidate(
        string title,
        IReadOnlyList<string> authors,
        IReadOnlyList<string>? isbns = null) =>
        new("test", "1", "Test", title, authors, [0xFF, 0xD8], 100, 150, isbns);
}

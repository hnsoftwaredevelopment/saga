using EbookManager.Application.Series;
using EbookManager.Domain.Series;
using FluentAssertions;

namespace EbookManager.Tests.Series;

public sealed class SeriesDuplicateFinderTests
{
    [Theory]
    [InlineData("Bob Evers", "Bob-Evers", true)]
    [InlineData("S.A.S.", "SAS", true)]
    [InlineData("Été", "Ete", true)]
    [InlineData("Serie 1", "Serie 2", false)]
    [InlineData("Serie 01", "Serie 1", false)]
    [InlineData("Serie Ⅰ", "Serie Ⅱ", false)]
    [InlineData("Serie ½", "Serie ¼", false)]
    [InlineData("Serie 𝟙", "Serie 𝟚", false)]
    [InlineData("Bob Evers", " bob evers ", false)]
    [InlineData("Bob Evers", "Bob Evers reeks", false)]
    public void Finds_only_equal_letter_and_number_sequences(string first, string second, bool expected)
        => SeriesDuplicateFinder.Find([new(first, 1), new(second, 2)]).Candidates.Count.Should().Be(expected ? 1 : 0);
    [Fact]
    public void Pairs_filter_both_names_and_report_limits_without_writing_or_transitive_merges()
    {
        SeriesSummary[] rows = [new("Bob Evers", 1), new("Bob-Evers", 2), new("Bob.Evers", 3)];
        SeriesDuplicateFinder.Find(rows).Candidates.Should().HaveCount(3);
        SeriesDuplicateFinder.Find(rows, "Bob-Evers").Candidates.Should().HaveCount(2);
        SeriesDuplicateFinder.Find(rows, "absent").Candidates.Should().BeEmpty();
        SeriesDuplicateFinder.Find(rows, limit: 1).Truncated.Should().BeTrue();
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        FluentActions.Invoking(() => SeriesDuplicateFinder.Find(rows, token: cancelled.Token)).Should().Throw<OperationCanceledException>();
    }
}

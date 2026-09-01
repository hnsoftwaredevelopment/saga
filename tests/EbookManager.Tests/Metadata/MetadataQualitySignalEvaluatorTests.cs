using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using FluentAssertions;

namespace EbookManager.Tests.Metadata;

public sealed class MetadataQualitySignalEvaluatorTests
{
    [Fact]
    public void Evaluate_returns_all_applicable_stable_signal_keys()
    {
        var book = CreateBook(
            title: "Jan Jansen",
            authors: ["Unknown"],
            language: "fictional-language",
            tags: [" rommelig, tag  naam "],
            seriesNumber: 2);

        var signals = MetadataQualitySignalEvaluator.Evaluate(book);

        signals.Should().BeEquivalentTo(
        [
            MetadataQualitySignalKeys.MissingAuthor,
            MetadataQualitySignalKeys.UnknownLanguage,
            MetadataQualitySignalKeys.MissingCover,
            MetadataQualitySignalKeys.SeriesNumberWithoutSeries,
            MetadataQualitySignalKeys.PossibleTitleAuthorSwap,
            MetadataQualitySignalKeys.MessyTags
        ]);
    }

    [Fact]
    public void Evaluate_returns_no_signals_for_complete_metadata()
    {
        var book = CreateBook(
            title: "Dune",
            authors: ["Frank Herbert"],
            language: "en",
            tags: ["Science fiction"],
            series: "Dune",
            seriesNumber: 1,
            coverBytes: [1]);

        MetadataQualitySignalEvaluator.Evaluate(book).Should().BeEmpty();
    }

    [Theory]
    [InlineData(MetadataQualitySignalKeys.MissingAuthor)]
    [InlineData(MetadataQualitySignalKeys.UnknownLanguage)]
    [InlineData(MetadataQualitySignalKeys.MissingCover)]
    [InlineData(MetadataQualitySignalKeys.SeriesNumberWithoutSeries)]
    [InlineData(MetadataQualitySignalKeys.PossibleTitleAuthorSwap)]
    [InlineData(MetadataQualitySignalKeys.MessyTags)]
    public void Applies_matches_the_evaluated_signal_set(string signalKey)
    {
        var book = CreateBook(
            title: "Jan Jansen",
            authors: ["Unknown"],
            language: null,
            tags: ["tag, tweede"],
            seriesNumber: 3);

        MetadataQualitySignalEvaluator.Applies(book, signalKey).Should().BeTrue();
    }

    private static Book CreateBook(
        string title,
        IReadOnlyList<string> authors,
        string? language,
        IReadOnlyList<string>? tags,
        string? series = null,
        decimal? seriesNumber = null,
        byte[]? coverBytes = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata(
                title,
                authors,
                Language: language,
                Tags: tags,
                Series: series,
                SeriesNumber: seriesNumber,
                CoverBytes: coverBytes),
            ReadingStatus.Unread,
            null,
            now,
            now);
    }
}

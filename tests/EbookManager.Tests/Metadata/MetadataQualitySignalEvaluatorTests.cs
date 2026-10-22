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

    [Theory]
    [InlineData("De Wandelaar", "Vreemdgaan")]
    [InlineData("De Zes Zwijgers", "Seamark")]
    [InlineData("Dokter Deen", "Edwin de Vries")]
    [InlineData("Dokter Finley Assistent", "A.J. Cronin")]
    [InlineData("Dolfje Sneeuwwolfje", "Paul van Loon")]
    [InlineData("Dolfje Weerwolfje: Boze Drieling", "Paul van Loon")]
    [InlineData("Dolle Dinsdag", "Nicolline van der Spek")]
    [InlineData("Don Abbondio", "A.L.G. Bosboom-Toussaint")]
    [InlineData("Dood Henry Kissinger", "Gérard de Villiers")]
    [InlineData("Dood Joesjtsjenko!", "Gérard de Villiers")]
    [InlineData("Dood Van Een Kater", "Arthur C. Clarke")]
    [InlineData("Dossier Blaauw", "J.A. Blaauw")]
    [InlineData("Dossier Gifmoord", "Ed van Eeden")]
    [InlineData("Dossier Karadzic", "Gérard de Villiers")]
    [InlineData("Dossier Kinderroof", "Ed van Eeden")]
    [InlineData("Dossier Tobias", "Annet de Jong")]
    [InlineData("Dossier Vrouwenhandel", "Ed van Eeden")]
    [InlineData("Dossier Zwarte Lotto", "Ed van Eeden")]
    [InlineData("Dubbele Agenda", "Saskia M.N. Oudshoorn")]
    [InlineData("Duistere Wraak", "Constance M. Burge")]
    [InlineData("Ebony Eyes", "Vorser")]
    [InlineData("Echt Sanne", "Marjan van den Berg")]
    [InlineData("Edelman, Bedelman, Schutter, Spion", "John le Carré")]
    [InlineData("Een Demonische Grappenmaker", "Constance M. Burge")]
    [InlineData("Een Duistere Erfenis", "Constance M. Burge")]
    [InlineData("Een Festival Vol Verwikkelingen", "Peter de Zwaan")]
    [InlineData("Een Godsoordeel", "Mr. J. van Lennep")]
    [InlineData("Een Nachtmerrie", "J.H. Carroll")]
    [InlineData("Een Oase Van Verhalen", "Adriaan van Garde")]
    [InlineData("Een Oude Kennis", "hertaald door Hans Keuning Hilbrand")]
    [InlineData("Een Vermoeden Van Vrijheid", "Henk de Velde")]
    [InlineData("Een Wraakzuchtig Verleden", "Constance M. Burge")]
    [InlineData("Jan Jansen", "Unknown")]
    [InlineData("Jan Jansen", "paul van loon")]
    [InlineData("Jan Jansen", "De Vries")]
    [InlineData("Jan Jansen", "De vries")]
    [InlineData("Jan Jansen", "")]
    [InlineData("Jan Jansen", "Maria Garcia")]
    [InlineData("", "De wereld vergeten")]
    [InlineData(" \t ", "De wereld vergeten")]
    [InlineData("Een, Twee, Drie", "De wereld vergeten")]
    [InlineData("Jan Jansen", "De Wereld Vergeten")]
    public void Swap_does_not_flag_correct_or_ambiguous_metadata(string title, string author)
    {
        var book = CreateBook(title, [author], "nl", []);

        MetadataQualitySignalEvaluator.Applies(book, MetadataQualitySignalKeys.PossibleTitleAuthorSwap)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData("Duenas, Maria", "De wereld vergeten")]
    [InlineData("Jan Jansen", "De verdwenen stad")]
    [InlineData("A.J. Cronin", "Het verloren boek")]
    [InlineData("Paul van Loon", "Een mooie dag")]
    [InlineData("John le Carré", "The forgotten city")]
    [InlineData("A.L.G. Bosboom-Toussaint", "De wereld vergeten")]
    [InlineData("Mr. J. van Lennep", "De wereld vergeten")]
    [InlineData(" Duenas,  Maria ", " De wereld vergeten ")]
    public void Swap_keeps_supported_swaps_and_disappears_after_correction(string title, string author)
    {
        MetadataQualitySignalEvaluator.Applies(CreateBook(title, [author], "nl", []),
                MetadataQualitySignalKeys.PossibleTitleAuthorSwap).Should().BeTrue();
        MetadataQualitySignalEvaluator.Applies(CreateBook(author, [title], "nl", []),
                MetadataQualitySignalKeys.PossibleTitleAuthorSwap).Should().BeFalse();
    }

    [Fact]
    public void Swap_does_not_flag_multiple_authors()
    {
        var book = CreateBook("Jan Jansen", ["De verdwenen stad", "Het verloren boek"], "nl", []);

        MetadataQualitySignalEvaluator.Applies(book, MetadataQualitySignalKeys.PossibleTitleAuthorSwap)
            .Should().BeFalse();
    }

    [Fact]
    public void Swap_leaves_a_one_word_author_to_manual_review_even_when_it_is_a_book_title()
    {
        // Without a name dictionary, a title such as Triptiek cannot be safely
        // distinguished from a single-word author such as Seamark or Vorser.
        var book = CreateBook("Karin Slaughter", ["Triptiek"], "nl", []);

        MetadataQualitySignalEvaluator.Applies(book, MetadataQualitySignalKeys.PossibleTitleAuthorSwap)
            .Should().BeFalse();
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

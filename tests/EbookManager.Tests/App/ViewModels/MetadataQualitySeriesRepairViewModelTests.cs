using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class MetadataQualitySeriesRepairViewModelTests
{
    [Fact]
    public void Constructor_normalizes_known_series_and_shows_the_existing_number()
    {
        var viewModel = new MetadataQualitySeriesRepairViewModel(
            "Boektitel",
            2.5m,
            ["  Dune ", "", "dune", "Foundation"]);

        viewModel.BookTitle.Should().Be("Boektitel");
        viewModel.SeriesNumber.Should().Be(2.5m);
        viewModel.Suggestions.Should().Equal("Dune", "Foundation");
    }

    [Fact]
    public void Bulk_context_shows_the_count_and_preserves_individual_series_numbers()
    {
        var viewModel = new MetadataQualitySeriesRepairViewModel(
            [("Eerste boek", 1m), ("Tweede boek", 2.5m)],
            ["Discworld"],
            Localize);

        viewModel.AffectedBookCount.Should().Be(2);
        viewModel.BookTitle.Should().BeNull();
        viewModel.SeriesNumber.Should().BeNull();
        viewModel.ContextLabel.Should().Be("Geselecteerde boeken");
        viewModel.ContextText.Should().Be("2 geselecteerde boeken");
        viewModel.SeriesNumberLabel.Should().Be("Serienummers");
        viewModel.SeriesNumberText.Should().Be("Ieder boek behoudt zijn huidige serienummer.");
        viewModel.SaveButtonText.Should().Be("Serie wijzigen voor 2 boeken");
    }

    [Fact]
    public void Single_context_keeps_the_existing_title_number_and_save_text()
    {
        var viewModel = new MetadataQualitySeriesRepairViewModel(
            [("Boektitel", 3m)],
            [],
            Localize);

        viewModel.AffectedBookCount.Should().Be(1);
        viewModel.BookTitle.Should().Be("Boektitel");
        viewModel.SeriesNumber.Should().Be(3m);
        viewModel.ContextLabel.Should().Be("Titel");
        viewModel.ContextText.Should().Be("Boektitel");
        viewModel.SeriesNumberLabel.Should().Be("Serienummer");
        viewModel.SeriesNumberText.Should().Be("3");
        viewModel.SaveButtonText.Should().Be("Serie wijzigen");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_series_cannot_be_saved(string series)
    {
        var viewModel = new MetadataQualitySeriesRepairViewModel("Boek", 1, []);

        viewModel.SeriesText = series;

        viewModel.CanSave.Should().BeFalse();
        viewModel.NormalizedSeries.Should().BeNull();
    }

    [Fact]
    public void New_series_is_trimmed_and_can_be_saved()
    {
        var viewModel = new MetadataQualitySeriesRepairViewModel("Boek", 1, []);

        viewModel.SeriesText = "  Nieuwe serie  ";

        viewModel.CanSave.Should().BeTrue();
        viewModel.NormalizedSeries.Should().Be("Nieuwe serie");
    }

    [Fact]
    public void Suggestions_prioritize_prefix_matches_before_partial_matches()
    {
        var viewModel = new MetadataQualitySeriesRepairViewModel(
            "Boek",
            1,
            ["The Expanse", "Expanse Legacy", "The Expense", "Dune"]);

        viewModel.SeriesText = "exp";

        viewModel.Suggestions.Should().Equal("Expanse Legacy", "The Expanse", "The Expense");
    }

    [Fact]
    public void UseSuggestion_preserves_the_known_spelling()
    {
        var viewModel = new MetadataQualitySeriesRepairViewModel("Boek", 1, ["Discworld"]);
        viewModel.SeriesText = "disc";

        viewModel.UseSuggestion(viewModel.Suggestions.Single());

        viewModel.SeriesText.Should().Be("Discworld");
    }

    private static string Localize(string key) => key switch
    {
        "Title" => "Titel",
        "SeriesNumber" => "Serienummer",
        "MetadataMultiEditSelectedBooks" => "Geselecteerde boeken",
        "MetadataQualitySeriesRepairBulkBookContext" => "{0} geselecteerde boeken",
        "MetadataQualitySeriesRepairBulkSeriesNumbersLabel" => "Serienummers",
        "MetadataQualitySeriesRepairBulkSeriesNumbersHelp" =>
            "Ieder boek behoudt zijn huidige serienummer.",
        "MetadataQualitySeriesRepairSave" => "Serie wijzigen",
        "MetadataQualitySeriesRepairBulkSave" => "Serie wijzigen voor {0} boeken",
        _ => key
    };
}

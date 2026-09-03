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
}

using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class MetadataQualityAuthorRepairViewModelTests
{
    [Fact]
    public void Constructor_normalizes_known_authors_and_excludes_unusable_values()
    {
        var viewModel = new MetadataQualityAuthorRepairViewModel(
            "Boektitel",
            ["  Karin Slaughter ", "", "Unknown", "karin slaughter", "Lee Child"]);

        viewModel.BookTitle.Should().Be("Boektitel");
        viewModel.Suggestions.Should().Equal("Karin Slaughter", "Lee Child");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Unknown")]
    [InlineData("unknown")]
    public void Invalid_author_cannot_be_saved(string author)
    {
        var viewModel = new MetadataQualityAuthorRepairViewModel("Boek", []);

        viewModel.AuthorText = author;

        viewModel.CanSave.Should().BeFalse();
        viewModel.NormalizedAuthor.Should().BeNull();
    }

    [Fact]
    public void New_author_is_trimmed_and_can_be_saved()
    {
        var viewModel = new MetadataQualityAuthorRepairViewModel("Boek", []);

        viewModel.AuthorText = "  Nieuwe Auteur  ";

        viewModel.CanSave.Should().BeTrue();
        viewModel.NormalizedAuthor.Should().Be("Nieuwe Auteur");
    }

    [Fact]
    public void Suggestions_prioritize_prefix_matches_before_other_partial_matches()
    {
        var viewModel = new MetadataQualityAuthorRepairViewModel(
            "Boek",
            ["Terry Pratchett", "Patrick Rothfuss", "Pat Barker", "Alan Paton", "Patricia Highsmith"]);

        viewModel.AuthorText = "pat";

        viewModel.Suggestions.Should().Equal(
            "Pat Barker",
            "Patricia Highsmith",
            "Patrick Rothfuss",
            "Alan Paton");
    }

    [Fact]
    public void Suggestions_are_empty_when_no_known_author_matches()
    {
        var viewModel = new MetadataQualityAuthorRepairViewModel("Boek", ["Auteur A", "Auteur B"]);

        viewModel.AuthorText = "Nieuwe";

        viewModel.Suggestions.Should().BeEmpty();
        viewModel.CanSave.Should().BeTrue();
    }

    [Fact]
    public void UseSuggestion_preserves_the_known_spelling()
    {
        var viewModel = new MetadataQualityAuthorRepairViewModel("Boek", ["Åke Edwardson"]);
        viewModel.AuthorText = "åke";

        viewModel.UseSuggestion(viewModel.Suggestions.Single());

        viewModel.AuthorText.Should().Be("Åke Edwardson");
        viewModel.NormalizedAuthor.Should().Be("Åke Edwardson");
    }
}

using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class MetadataQualityTagRepairViewModelTests
{
    [Fact]
    public void Constructor_shows_current_tags_and_builds_an_editable_clean_proposal()
    {
        var viewModel = new MetadataQualityTagRepairViewModel(
            "Boek",
            [" Thriller ", "Misdaad, Spanning", "thriller", " "]);

        viewModel.BookTitle.Should().Be("Boek");
        viewModel.CurrentTagsText.Should().Be(
            string.Join(Environment.NewLine, " Thriller ", "Misdaad, Spanning", "thriller", " "));
        viewModel.TagsText.Should().Be(
            string.Join(Environment.NewLine, "Thriller", "Misdaad", "Spanning"));
        viewModel.NormalizedTags.Should().Equal("Thriller", "Misdaad", "Spanning");
        viewModel.CanSave.Should().BeTrue();
    }

    [Fact]
    public void Editing_the_proposal_reuses_the_canonical_normalization()
    {
        var viewModel = new MetadataQualityTagRepairViewModel("Boek", ["Tag, Tweede"]);

        viewModel.TagsText = "  Nieuw   label  \r\nTweede, nieuw LABEL";

        viewModel.NormalizedTags.Should().Equal("Nieuw label", "Tweede");
        viewModel.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_is_false_when_the_normalized_tags_equal_the_stored_tags()
    {
        var viewModel = new MetadataQualityTagRepairViewModel("Boek", ["Eerste", "Tweede"]);

        viewModel.CanSave.Should().BeFalse();

        viewModel.TagsText = "eerste\r\nTweede";

        viewModel.CanSave.Should().BeFalse();
    }

    [Fact]
    public void An_empty_proposal_can_remove_an_existing_blank_tag()
    {
        var viewModel = new MetadataQualityTagRepairViewModel("Boek", [" "]);

        viewModel.TagsText.Should().BeEmpty();
        viewModel.NormalizedTags.Should().BeEmpty();
        viewModel.CanSave.Should().BeTrue();
    }
}

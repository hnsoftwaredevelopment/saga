using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class MetadataQualityTitleAuthorRepairViewModelTests
{
    [Fact]
    public void Constructor_exposes_an_unambiguous_before_and_after_preview()
    {
        var viewModel = new MetadataQualityTitleAuthorRepairViewModel(
            "  Jan Jansen  ",
            "  De verdwenen stad  ");

        viewModel.CurrentTitle.Should().Be("Jan Jansen");
        viewModel.CurrentAuthor.Should().Be("De verdwenen stad");
        viewModel.NewTitle.Should().Be("De verdwenen stad");
        viewModel.NewAuthor.Should().Be("Jan Jansen");
    }

    [Theory]
    [InlineData("", "Auteur")]
    [InlineData("Titel", "")]
    [InlineData("Titel", "Unknown")]
    public void Constructor_rejects_values_that_cannot_be_safely_swapped(
        string title,
        string author)
    {
        var action = () => new MetadataQualityTitleAuthorRepairViewModel(title, author);

        action.Should().Throw<ArgumentException>();
    }
}

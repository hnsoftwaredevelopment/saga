using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class MetadataQualityLanguageRepairViewModelTests
{
    [Fact]
    public void Constructor_offers_distinct_valid_languages_with_display_names()
    {
        var viewModel = new MetadataQualityLanguageRepairViewModel("Boektitel");

        viewModel.BookTitle.Should().Be("Boektitel");
        viewModel.Languages.Should().Contain(option => option.Code == "nl");
        viewModel.Languages.Should().Contain(option => option.Code == "en");
        viewModel.Languages.Select(option => option.Code)
            .Should().OnlyHaveUniqueItems();
        viewModel.Languages.Should().OnlyContain(option =>
            !string.IsNullOrWhiteSpace(option.DisplayName));
    }

    [Fact]
    public void A_language_must_be_selected_before_saving()
    {
        var viewModel = new MetadataQualityLanguageRepairViewModel("Boek");

        viewModel.CanSave.Should().BeFalse();
        viewModel.NormalizedLanguage.Should().BeNull();

        viewModel.SelectedLanguage = viewModel.Languages.Single(option => option.Code == "nl");

        viewModel.CanSave.Should().BeTrue();
        viewModel.NormalizedLanguage.Should().Be("nl");
    }
}

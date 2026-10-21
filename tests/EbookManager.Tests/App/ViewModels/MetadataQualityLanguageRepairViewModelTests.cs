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
        viewModel.AffectedBookCount.Should().Be(1);
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

    [Fact]
    public void Single_book_context_shows_the_title_and_normal_change_action()
    {
        var viewModel = new MetadataQualityLanguageRepairViewModel(
            ["De Hobbit"],
            Localize);

        viewModel.AffectedBookCount.Should().Be(1);
        viewModel.BookTitle.Should().Be("De Hobbit");
        viewModel.ContextLabel.Should().Be("Title");
        viewModel.ContextText.Should().Be("De Hobbit");
        viewModel.SaveButtonText.Should().Be("Change language");
    }

    [Fact]
    public void Multiple_book_context_shows_the_count_and_explicit_bulk_action()
    {
        var viewModel = new MetadataQualityLanguageRepairViewModel(
            ["Eerste boek", "Tweede boek", "Derde boek"],
            Localize);

        viewModel.AffectedBookCount.Should().Be(3);
        viewModel.BookTitle.Should().BeNull();
        viewModel.ContextLabel.Should().Be("Selected books");
        viewModel.ContextText.Should().Be("3 books selected");
        viewModel.SaveButtonText.Should().Be("Change language for 3 books");
    }

    private static string Localize(string key) => key switch
    {
        "Title" => "Title",
        "MetadataMultiEditSelectedBooks" => "Selected books",
        "MetadataQualityLanguageRepairSave" => "Change language",
        "MetadataQualityLanguageRepairBulkBookContext" => "{0} books selected",
        "MetadataQualityLanguageRepairBulkSave" => "Change language for {0} books",
        _ => key
    };
}

using EbookManager.Presentation.ViewModels;
using EbookManager.Domain.Books;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class MetadataQualityTitleAuthorRepairViewModelTests
{
    [Fact]
    public void Bulk_confirmation_uses_the_existing_selection_and_skips_multiple_authors()
    {
        var now = DateTimeOffset.UtcNow;
        var first = new Book(Guid.NewGuid(), new BookMetadata("Jan Jansen", ["De verdwenen stad"]),
            ReadingStatus.Unread, null, now, now);
        var second = first with { Id = Guid.NewGuid(), Metadata = new BookMetadata("Piet Peters", ["Een boek", "Een ander"]) };
        var repair = new MetadataQualityTitleAuthorRepairViewModel([first, second],
            key => key == "MetadataQualityTitleAuthorRepairBulkConfirmationSkipped" ? "{0}:{1}:{2}" : key);

        repair.IsBulk.Should().BeTrue();
        repair.CanConfirm.Should().BeTrue();
        repair.Previews[0].NewTitle.Should().Be("De verdwenen stad");
        repair.Previews[0].NewAuthor.Should().Be("Jan Jansen");
        repair.Previews[1].CanRepair.Should().BeFalse();
        repair.EligibleCount.Should().Be(1);
        repair.BulkConfirmationMessage.Should().Be("2:1:1");
    }

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

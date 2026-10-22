using EbookManager.Presentation.ViewModels;
using EbookManager.Domain.Books;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class MetadataQualityTitleAuthorRepairViewModelTests
{
    [Fact]
    public void Bulk_preview_requires_an_explicit_choice_and_rejects_multiple_authors()
    {
        var now = DateTimeOffset.UtcNow;
        var first = new Book(Guid.NewGuid(), new BookMetadata("Jan Jansen", ["De verdwenen stad"]),
            ReadingStatus.Unread, null, now, now);
        var second = first with { Id = Guid.NewGuid(), Metadata = new BookMetadata("Piet Peters", ["Een boek", "Een ander"]) };
        var repair = new MetadataQualityTitleAuthorRepairViewModel([first, second], key => key);
        var notifications = new List<string?>();
        repair.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        repair.IsBulk.Should().BeTrue();
        repair.SelectedCount.Should().Be(0);
        repair.CanConfirm.Should().BeFalse();
        repair.Previews[0].NewTitle.Should().Be("De verdwenen stad");
        repair.Previews[0].NewAuthor.Should().Be("Jan Jansen");
        repair.Previews[1].CanRepair.Should().BeFalse();
        repair.Previews[1].IsSelected = true;
        repair.CanConfirm.Should().BeFalse();

        repair.Previews[0].IsSelected = true;
        repair.SelectedCount.Should().Be(1);
        repair.CanConfirm.Should().BeTrue();
        notifications.Should().Contain(nameof(repair.CanConfirm));
        repair.Previews[0].IsSelected = false;
        repair.CanConfirm.Should().BeFalse();
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

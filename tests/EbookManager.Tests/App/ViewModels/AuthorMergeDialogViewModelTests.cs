using EbookManager.Domain.Authors;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class AuthorMergeDialogViewModelTests
{
    [Fact]
    public void Comparison_cards_show_full_values_and_choose_the_visible_sources_without_changing_other_fields()
    {
        var first = new AuthorProfile(Guid.NewGuid(), "First", new string('a', 500), "https://first.example", "first.jpg");
        var second = new AuthorProfile(Guid.NewGuid(), "Second", "Second biography", null, null);
        var vm = new AuthorMergeDialogViewModel(new([new(first, [Guid.NewGuid()]), new(second, [])]), key => key);
        vm.ComparisonAuthors[0].Profile.Biography.Should().HaveLength(500);
        vm.ComparisonAuthors[0].BookCount.Should().Be(1);
        vm.UseTargetCommand.Execute(vm.ComparisonAuthors[1]);
        vm.UseBiographyCommand.Execute(vm.ComparisonAuthors[0]);
        vm.UseWebsiteCommand.Execute(vm.ComparisonAuthors[1]);
        vm.UsePhotoCommand.Execute(vm.ComparisonAuthors[0]);
        vm.Selection.Should().Be(new AuthorMergeSelection(second.Id, first.Id, null, first.Id));
        vm.BiographyChoice!.Value.Should().Be(first.Biography);
        vm.UsePhotoCommand.Execute(vm.ComparisonAuthors[1]);
        vm.Selection.PhotoAuthorId.Should().BeNull();
        vm.Selection.BiographyAuthorId.Should().Be(first.Id);
        vm.PhotoLoading = true; vm.CanMerge.Should().BeFalse();
        vm.PhotoLoading = false; vm.CanMerge.Should().BeTrue();
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AuthorMerge_dialog_requires_target_and_explicit_conflicting_profile_choices(bool conflicting)
    {
        var first = new AuthorProfile(Guid.NewGuid(), "First", "Biography", "https://example.org", "first.jpg");
        var second = new AuthorProfile(Guid.NewGuid(), "Second", conflicting ? "Different" : "Biography", null, conflicting ? "second.jpg" : null);
        var shared = Guid.NewGuid();
        var vm = new AuthorMergeDialogViewModel(new([new(first, [shared]), new(second, [shared, Guid.NewGuid()])]), key => key);
        vm.Preview.BookCount.Should().Be(2);
        vm.CanMerge.Should().BeFalse();
        vm.Target = vm.Authors[1];
        if (conflicting)
        {
            vm.CanMerge.Should().BeFalse();
            vm.BiographyChoice.Should().BeNull();
            vm.PhotoChoice.Should().BeNull();
            vm.BiographyChoice = vm.BiographyOptions[0];
            vm.PhotoChoice = vm.PhotoOptions[1];
        }
        vm.CanMerge.Should().BeTrue();
        vm.Selection.TargetId.Should().Be(second.Id);
        vm.Selection.WebsiteAuthorId.Should().Be(first.Id);
        vm.Target = new(Guid.NewGuid(), "Outside selection", 1);
        vm.CanMerge.Should().BeFalse();
    }
}

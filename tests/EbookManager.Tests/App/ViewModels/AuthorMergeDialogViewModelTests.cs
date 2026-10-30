using EbookManager.Domain.Authors;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class AuthorMergeDialogViewModelTests
{
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

using EbookManager.Domain.Authors;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class AuthorRenameDialogViewModelTests
{
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Unknown")]
    [InlineData(" Original ")]
    public void Invalid_or_unchanged_name_cannot_be_confirmed(string name)
    {
        var vm = new AuthorRenameDialogViewModel(new(new(Guid.NewGuid(), "Original", null, null, null), []), key => key);
        vm.NewName = name;
        vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void Valid_name_and_case_only_correction_can_be_confirmed_with_book_count()
    {
        var vm = new AuthorRenameDialogViewModel(new(new(Guid.NewGuid(), "Original", null, null, null), [Guid.NewGuid()]), key => key);
        vm.BookCount.Should().Be(1);
        vm.NewName = "ORIGINAL";
        vm.CanSave.Should().BeTrue();
        vm.NewName = "New name";
        vm.CanSave.Should().BeTrue();
    }
}

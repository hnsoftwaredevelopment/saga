using EbookManager.Domain.Tags;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class TagAssignmentDialogViewModelTests
{
    [Theory]
    [InlineData("Source", false)]
    [InlineData(" source ", false)]
    [InlineData("Target", true)]
    [InlineData("", false)]
    [InlineData("A;B", false)]
    [InlineData("A\nB", false)]
    public void Replacement_requires_one_distinct_valid_name(string name, bool expected)
    {
        var vm = new TagAssignmentDialogViewModel(new(Guid.NewGuid(), "Source", [new(Guid.NewGuid(), "Book")]),
            TagAssignmentOperation.Replace, key => key);
        vm.NewName = name;
        vm.CanApply.Should().Be(expected);
        vm.HasTargetName.Should().BeTrue();
    }

    [Fact]
    public void Removal_has_no_name_input_and_requires_selected_books()
    {
        var vm = new TagAssignmentDialogViewModel(new(Guid.NewGuid(), "Source", [new(Guid.NewGuid(), "Book")]),
            TagAssignmentOperation.Remove, key => key);
        vm.HasTargetName.Should().BeFalse();
        vm.CanApply.Should().BeTrue();
        var empty = new TagAssignmentDialogViewModel(new(Guid.NewGuid(), "Source", []), TagAssignmentOperation.Remove, key => key);
        empty.CanApply.Should().BeFalse();
    }
}

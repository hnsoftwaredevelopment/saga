using EbookManager.Domain.Tags;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class TagAssignmentDialogViewModelTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData("Roman", false)]
    [InlineData("Roman\r\n roman ", false)]
    [InlineData("Roman\n Source", false)]
    [InlineData("Roman\nA;B", false)]
    [InlineData(" Roman\r\n\r\n Avontuur ", true)]
    public void Split_previews_distinct_replacements_and_rejects_invalid_entries(string input, bool expected)
    {
        var book = new TagSourceBook(Guid.NewGuid(), "Book");
        var vm = new TagAssignmentDialogViewModel(new(Guid.NewGuid(), "Source", [book]), TagAssignmentOperation.Split, key => key);
        vm.SplitText = input;
        vm.CanApply.Should().Be(expected);
        vm.SplitBooks.Should().Equal(book);
        if (expected)
        {
            vm.SplitPreview.Should().Be(string.Join(Environment.NewLine, "Roman", "Avontuur"));
            vm.CreateRequest().TargetNames.Should().Equal("Roman", "Avontuur");
        }
        else vm.SplitPreview.Should().BeEmpty();
    }
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

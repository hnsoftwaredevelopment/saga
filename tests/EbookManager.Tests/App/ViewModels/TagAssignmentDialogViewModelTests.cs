using EbookManager.Domain.Tags;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class TagAssignmentDialogViewModelTests
{
    [Fact]
    public void Add_requires_an_explicit_proposal_and_deduplicates_queued_tags()
    {
        var vm = new TagAssignmentDialogViewModel(new(Guid.NewGuid(), "Source", [new(Guid.NewGuid(), "Book")]),
            TagAssignmentOperation.Add, key => key);
        vm.NewName.Should().BeEmpty();
        vm.CanApply.Should().BeFalse();
        vm.NewName = "  Roman  ";
        vm.QueueTag(vm.NewName);
        vm.QueueTag(" roman ");
        vm.QueueTag("A;B");
        vm.CanApply.Should().BeTrue();
        vm.NewName = "Avontuur";
        vm.CanApply.Should().BeFalse();
        vm.QueueTag(vm.NewName);
        vm.CreateRequest().TargetNames.Should().Equal("Roman", "Avontuur");
        vm.UnqueueTag("Roman");
        vm.CreateRequest().TargetNames.Should().Equal("Avontuur");
        vm.UnqueueTag("Avontuur");
        vm.CanApply.Should().BeFalse();
    }

    [Fact]
    public void Pasting_tag_lines_preserves_spaces_deduplicates_and_rejects_invalid_lines_atomically()
    {
        var vm = new TagAssignmentDialogViewModel(new(Guid.NewGuid(), "Source", [new(Guid.NewGuid(), "Book")]),
            TagAssignmentOperation.Add, key => key);
        vm.QueueTag("Historische roman");
        vm.QueuePastedTags(" historische roman \r\n\r\n Science Fiction \nJeugd 12+").Should().BeTrue();
        vm.CreateRequest().TargetNames.Should().Equal("Historische roman", "Science Fiction", "Jeugd 12+");
        vm.NewName = "Draft";
        vm.QueuePastedTags("New\nA;B").Should().BeFalse();
        vm.NewName.Should().Be("Draft");
        vm.CreateRequest().TargetNames.Should().Equal("Historische roman", "Science Fiction", "Jeugd 12+");
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

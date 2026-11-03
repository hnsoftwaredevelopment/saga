using EbookManager.Domain.Series;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.Series;

public sealed class ManualSeriesMergeDialogTests
{
    [Fact]
    public void Existing_and_new_name_choices_are_explicit_mutually_exclusive_and_trimmed()
    {
        var preview = new SeriesMergePreview([new("One", []), new("Two", []), new("Three", [])], true);
        var vm = new SeriesMergeDialogViewModel(preview, key => key);
        vm.CanUseNewName.Should().BeTrue(); vm.CanMerge.Should().BeFalse();
        vm.SelectedTarget = preview.Groups[1]; vm.CanMerge.Should().BeTrue(); vm.TargetName.Should().Be("Two");
        vm.NewNameText = "  New series  "; vm.SelectedTarget.Should().BeNull(); vm.TargetName.Should().Be("New series"); vm.CanMerge.Should().BeTrue();
        vm.NewNameText = "   "; vm.CanMerge.Should().BeFalse(); vm.TargetName.Should().BeEmpty();
        vm.SelectedTarget = preview.Groups[0]; vm.NewNameText.Should().BeEmpty(); vm.TargetName.Should().Be("One");
        vm.NewNameText = ""; vm.TargetName.Should().Be("One");
    }
    [Fact]
    public void Duplicate_suggestion_mode_keeps_existing_target_requirement_and_original_numbers()
    {
        var book = new SeriesMergeBook(Guid.NewGuid(), "One", "Title", "Author", 1.5m);
        var preview = new SeriesMergePreview([new("One", [book]), new("Two", [])]);
        var vm = new SeriesMergeDialogViewModel(preview, key => key) { NewNameText = "New series" };
        vm.CanUseNewName.Should().BeFalse(); vm.CanMerge.Should().BeFalse();
        vm.SelectedTarget = preview.Groups[0]; vm.TargetName.Should().Be("One"); vm.NewNameText.Should().BeEmpty();
        vm.Books.Single().Number.Should().Be(1.5m); book.Number.Should().Be(1.5m);
    }
}

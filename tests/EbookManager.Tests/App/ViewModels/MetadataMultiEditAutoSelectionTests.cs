using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class MetadataMultiEditAutoSelectionTests
{
    [Theory]
    [InlineData("AuthorsText", "UpdateAuthors")]
    [InlineData("SeriesText", "UpdateSeries")]
    [InlineData("TagsText", "UpdateTags")]
    [InlineData("LanguageText", "UpdateLanguage")]
    public void Typing_selects_the_field_and_clearing_deselects_it_without_losing_manual_control(string textProperty, string flagProperty)
    {
        var vm = new MetadataMultiEditViewModel(2);
        var text = typeof(MetadataMultiEditViewModel).GetProperty(textProperty)!;
        var flag = typeof(MetadataMultiEditViewModel).GetProperty(flagProperty)!;
        vm.HasSelectedChanges.Should().BeFalse();

        text.SetValue(vm, "New value");
        flag.GetValue(vm).Should().Be(true);
        vm.ApplyCommand.CanExecute(null).Should().BeTrue();

        flag.SetValue(vm, false);
        text.GetValue(vm).Should().Be("New value", "manually opting out does not discard the draft");
        typeof(MetadataMultiEditResult).GetProperty(flagProperty)!.GetValue(vm.CreateResult()).Should().Be(false);

        text.SetValue(vm, "More text");
        flag.GetValue(vm).Should().Be(true);
        text.SetValue(vm, "");
        flag.GetValue(vm).Should().Be(false);
        vm.HasSelectedChanges.Should().BeFalse();

        flag.SetValue(vm, true);
        text.GetValue(vm).Should().Be("");
        typeof(MetadataMultiEditResult).GetProperty(flagProperty)!.GetValue(vm.CreateResult()).Should().Be(true, "an explicitly selected empty field clears its stored value");
        vm.ApplyCommand.CanExecute(null).Should().BeTrue();

        text.SetValue(vm, "   ");
        flag.GetValue(vm).Should().Be(false);
    }

    [Fact]
    public void Typing_cannot_enable_author_replacement_while_swapping_title_and_authors()
    {
        var vm = new MetadataMultiEditViewModel(2) { SwapTitleAndAuthors = true };
        vm.AuthorsText = "A name";
        vm.UpdateAuthors.Should().BeFalse();
        vm.CreateResult().SwapTitleAndAuthors.Should().BeTrue();
    }
}

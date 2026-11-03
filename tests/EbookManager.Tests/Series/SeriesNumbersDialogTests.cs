using System.Globalization;
using EbookManager.Domain.Series;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.Series;

public sealed class SeriesNumbersDialogTests
{
    [Fact]
    public void Reordering_preserves_the_selected_row_and_proposes_numbers_in_the_visible_order()
    {
        var vm = new SeriesNumbersDialogViewModel(Preview(), key => key, CultureInfo.GetCultureInfo("nl-NL"));
        var first = vm.SelectedRow!; vm.MoveSelected(1);
        vm.SelectedRow.Should().BeSameAs(first); vm.Rows[1].Should().BeSameAs(first);
        vm.StartNumberText = "1,5";
        vm.Rows.Select(row => row.ProposedNumber).Should().Equal(1.5m, 2.5m, 3.5m);
        vm.CreateAssignments().Select(row => row.Id).Should().Equal(vm.Rows.Select(row => row.Source.Book.Id));
        vm.Rows.Select(row => row.CurrentNumber).Should().Equal(8m, 9m, null);
        vm.CanMoveUp.Should().BeTrue(); vm.CanMoveDown.Should().BeTrue();
    }
    [Theory]
    [InlineData("")][InlineData("letters")][InlineData("1.000")][InlineData("1e2")][InlineData("79228162514264337593543950335")]
    public void Invalid_or_overflowing_start_disables_confirmation_without_changing_original_numbers(string input)
    {
        var vm = new SeriesNumbersDialogViewModel(Preview(), key => key, CultureInfo.GetCultureInfo("nl-NL")) { StartNumberText = input };
        vm.CanSave.Should().BeFalse(); vm.ValidationMessage.Should().Be("SeriesNumbersInvalid");
        vm.Rows.Should().OnlyContain(row => row.ProposedNumber == null);
        vm.Rows.Select(row => row.CurrentNumber).Should().Equal(9m, 8m, null);
    }
    [Theory]
    [InlineData("en-US", "1.5", "1000.5")][InlineData("nl-NL", "1,5", "1000,5")]
    public void Decimals_follow_culture_and_duplicate_other_numbers_are_informational(string culture, string input, string large)
    {
        var vm = new SeriesNumbersDialogViewModel(Preview(), key => key, CultureInfo.GetCultureInfo(culture)) { StartNumberText = input };
        vm.DuplicateCount.Should().Be(1); vm.CanSave.Should().BeTrue();
        vm.StartNumberText = large; vm.Rows[0].ProposedNumber.Should().Be(1000.5m);
        vm.StartNumberText = "-1"; vm.Rows.Select(row => row.ProposedNumber).Should().Equal(-1m, 0m, 1m);
    }
    private static SeriesNumbersPreview Preview() => new("Series", [new("Series", new(Guid.NewGuid(), "First", "Author", 9)), new("Series", new(Guid.NewGuid(), "Second", "Author", 8)), new("Series", new(Guid.NewGuid(), "Third", "Author", null))], [1.5m, 99m]);
}

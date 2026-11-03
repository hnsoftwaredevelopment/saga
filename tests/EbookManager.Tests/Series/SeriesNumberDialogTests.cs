using EbookManager.Domain.Series;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;
using System.Globalization;

namespace EbookManager.Tests.Series;

public sealed class SeriesNumberDialogTests
{
    [Theory]
    [InlineData("nl-NL", "1,5", true, "1.5")]
    [InlineData("en-US", "1.5", true, "1.5")]
    [InlineData("nl-NL", "1.5", false, null)]
    [InlineData("en-US", "1,5", false, null)]
    [InlineData("nl-NL", "1.000", false, null)]
    [InlineData("nl-NL", "1e2", false, null)]
    [InlineData("nl-NL", "letters", false, null)]
    [InlineData("nl-NL", "99999999999999999999999999999999999999", false, null)]
    [InlineData("nl-NL", "", true, null)]
    [InlineData("nl-NL", "  ", true, null)]
    [InlineData("nl-NL", "0", true, "0")]
    [InlineData("nl-NL", "-1", true, "-1")]
    [InlineData("nl-NL", "2,00", false, "2")]
    public void Parses_local_decimal_or_empty_and_requires_a_real_change(string culture, string text, bool canSave, string? expected)
    {
        var vm = new SeriesNumberDialogViewModel(new("Series", new(Guid.NewGuid(), "Book", "Author", 2), [1.5m]), key => key, CultureInfo.GetCultureInfo(culture));
        vm.NumberText = text;
        vm.CanSave.Should().Be(canSave);
        if (canSave || expected is not null) vm.Number.Should().Be(expected is null ? null : decimal.Parse(expected, CultureInfo.InvariantCulture));
        vm.HasDuplicate.Should().Be(expected == "1.5");
    }

    [Fact]
    public void Duplicate_is_information_and_does_not_block_saving_or_count_empty_numbers()
    {
        var vm = new SeriesNumberDialogViewModel(new("Series", new(Guid.NewGuid(), "Book", "Author", 2), [1.5m, 1.5m]), key => key, CultureInfo.GetCultureInfo("nl-NL"));
        vm.NumberText = "1,5";
        vm.CanSave.Should().BeTrue(); vm.HasDuplicate.Should().BeTrue(); vm.DuplicateCount.Should().Be(2);
        vm.NumberText = "";
        vm.CanSave.Should().BeTrue(); vm.HasDuplicate.Should().BeFalse(); vm.ValidationMessage.Should().BeEmpty();
        vm.NumberText = "a";
        vm.CanSave.Should().BeFalse(); vm.ValidationMessage.Should().Be("SeriesNumberInvalid");
    }
}

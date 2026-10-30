using EbookManager.Domain.Series;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.Series;

public sealed class SeriesMergeDialogTests
{
    [Fact]
    public void Requires_existing_target_and_pages_numeric_order_without_changing_numbers()
    {
        var first = new SeriesMergeBook(Guid.NewGuid(), "Bob-Evers", "First", "Author", 1);
        var repeated = new SeriesMergeBook(Guid.NewGuid(), "Bob Evers", "Repeated", "Other", 1);
        var missing = new SeriesMergeBook(Guid.NewGuid(), "Bob Evers", "Missing", "Other", null);
        var rows = Enumerable.Range(2, 202).Select(number => new SeriesMergeBook(Guid.NewGuid(), "Bob-Evers", $"Book {number}", "Author", number)).Prepend(first).ToArray();
        var preview = new SeriesMergePreview([new("Bob-Evers", rows), new("Bob Evers", [repeated, missing])]);
        var vm = new SeriesMergeDialogViewModel(preview, key => key);
        vm.CanMerge.Should().BeFalse();
        vm.Books.Should().HaveCount(200);
        vm.Books.Take(2).Should().OnlyContain(row => row.Number == 1 && row.Note == "SeriesMergeRepeatedNumber");
        vm.LoadMore();
        vm.Books.Should().HaveCount(205);
        vm.Books.Last().Number.Should().BeNull();
        vm.Books.Last().Note.Should().Be("SeriesMergeMissingNumber");
        vm.HasMore.Should().BeFalse();
        vm.SelectedTarget = new("New name", []);
        vm.CanMerge.Should().BeFalse();
        vm.SelectedTarget = preview.Groups[1];
        vm.CanMerge.Should().BeTrue();
        vm.TargetName.Should().Be("Bob Evers");
        preview.Groups[0].Books.Should().Equal(rows);
    }
}

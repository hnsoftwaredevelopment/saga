using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Views;

public sealed class DuplicateCandidatesWindowLayoutTests
{
    [Fact]
    public void Selected_duplicate_pair_has_a_visible_accessible_merge_action()
    {
        var document = XDocument.Load(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "DuplicateCandidatesWindow.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var button = document.Descendants(presentation + "Button")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "MergeSelectedButton");

        button.Attribute("IsEnabled")!.Value.Should().Be("{Binding CanMergeSelectedCandidates}");
        button.Attribute("Click")!.Value.Should().Be("MergeSelectedCandidatesClicked");
        button.Attribute("AutomationProperties.Name")!.Value.Should().Be("{loc:Loc Merge}");
        button.Descendants(presentation + "TextBlock")
            .Single().Attribute("Text")!.Value.Should().Be("{loc:Loc Merge}");
    }
}

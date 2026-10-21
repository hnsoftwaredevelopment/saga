using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Views;

public sealed class BookDetailsViewLayoutTests
{
    [Fact]
    public void Cover_area_always_exposes_an_accessible_change_action_and_staged_preview()
    {
        var document = XDocument.Load(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "BookDetailsView.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        var button = document.Descendants(presentation + "Button")
            .Single(element => (string?)element.Attribute("Command") == "{Binding ChangeCoverCommand}");
        button.Attribute("Content")!.Value.Should().Be("{loc:Loc ChangeCover}");
        button.Attribute("AutomationProperties.Name")!.Value.Should().Be("{loc:Loc ChangeCover}");

        document.Descendants(presentation + "Image")
            .Any(element => element.Attribute("Source")?.Value.Contains("CoverBytes", StringComparison.Ordinal) == true)
            .Should().BeTrue();
    }

    [Fact]
    public void Title_area_exposes_an_accessible_icon_button_for_swapping_title_and_authors()
    {
        var document = XDocument.Load(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "BookDetailsView.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        var button = document.Descendants(presentation + "Button")
            .Single(element =>
                (string?)element.Attribute("Command") == "{Binding SwapTitleAndAuthorsCommand}");

        button.Attribute("ToolTip")!.Value.Should().Be("{loc:Loc SwapTitleAndAuthorsTooltip}");
        button.Attribute("AutomationProperties.Name")!.Value
            .Should().Be("{loc:Loc SwapTitleAndAuthorsTooltip}");
        button.Attribute("IsTabStop")?.Value.Should().NotBe("False");
        button.Descendants(presentation + "Path")
            .Should().ContainSingle()
            .Which.Attribute("Data")!.Value.Should().Be("{StaticResource SwapTitleAuthorIconGeometry}");
    }
}

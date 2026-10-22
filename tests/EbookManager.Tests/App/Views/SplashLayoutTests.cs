using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Views;

public sealed class SplashLayoutTests
{
    [Fact]
    public void Brand_heading_centers_title_and_subtitle_without_vertical_child_margins()
    {
        var document = XDocument.Load(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "Splash.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var heading = document.Descendants(presentation + "Grid")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "BrandHeading");
        heading.Element(presentation + "Grid.ColumnDefinitions")!
            .Elements(presentation + "ColumnDefinition")
            .Select(column => (string?)column.Attribute("Width"))
            .Should().Equal("Auto", "Auto");

        var textBlocks = heading.Elements(presentation + "TextBlock").ToArray();
        textBlocks.Should().HaveCount(2);
        textBlocks.Should().OnlyContain(text =>
            (string?)text.Attribute("VerticalAlignment") == "Center");
        textBlocks.Should().OnlyContain(text => HasNoVerticalMargin(text));

        var subtitle = textBlocks.Single(text =>
            (string?)text.Attribute(xaml + "Name") == "SubtitleText");
        var subtitleOffset = subtitle
            .Element(presentation + "TextBlock.RenderTransform")?
            .Element(presentation + "TranslateTransform");
        subtitleOffset.Should().NotBeNull();
        ((string?)subtitleOffset!.Attribute("Y")).Should().Be("6");
    }

    private static bool HasNoVerticalMargin(XElement element)
    {
        var values = ((string?)element.Attribute("Margin") ?? "0")
            .Split(',')
            .Select(value => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();
        return values.Length switch
        {
            1 => values[0] == 0,
            2 => values[1] == 0,
            4 => values[1] == 0 && values[3] == 0,
            _ => false
        };
    }
}

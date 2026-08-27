using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Views;

public sealed class MetadataQualityDashboardWindowLayoutTests
{
    [Fact]
    public void IssuePane_CanBeResizedWithoutCollapsingEitherPane()
    {
        var document = XDocument.Load(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "MetadataQualityDashboardWindow.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var issuePaneColumn = document
            .Descendants(presentation + "ColumnDefinition")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "IssuePaneColumn");
        var splitter = document
            .Descendants(presentation + "GridSplitter")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "IssuePaneSplitter");

        issuePaneColumn.Attribute("Width")?.Value.Should().Be("320");
        issuePaneColumn.Attribute("MinWidth")?.Value.Should().Be("240");
        splitter.Attribute("Grid.Column")?.Value.Should().Be("1");
        splitter.Attribute("ResizeDirection")?.Value.Should().Be("Columns");
        splitter.Attribute("ResizeBehavior")?.Value.Should().Be("PreviousAndNext");
        splitter.Attribute("Focusable")?.Value.Should().Be("True");
    }
}

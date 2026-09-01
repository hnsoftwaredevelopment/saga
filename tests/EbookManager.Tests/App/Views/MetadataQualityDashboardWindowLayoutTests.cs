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

        RequiredAttribute(issuePaneColumn, "Width").Should().Be("320");
        RequiredAttribute(issuePaneColumn, "MinWidth").Should().Be("240");
        RequiredAttribute(issuePaneColumn, "MaxWidth").Should().Be("600");
        RequiredAttribute(splitter, "Grid.Column").Should().Be("1");
        RequiredAttribute(splitter, "ResizeDirection").Should().Be("Columns");
        RequiredAttribute(splitter, "ResizeBehavior").Should().Be("PreviousAndNext");
        RequiredAttribute(splitter, "Focusable").Should().Be("True");
        RequiredAttribute(splitter, "Style").Should().Be("{StaticResource VisibleVerticalGridSplitterStyle}");
    }

    [Fact]
    public void MarkCorrectAction_IsCommandBoundAndKeyboardAccessible()
    {
        var document = XDocument.Load(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "MetadataQualityDashboardWindow.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var button = document
            .Descendants(presentation + "Button")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "MarkSelectedIssueCorrectButton");

        RequiredAttribute(button, "Content").Should().Be("{loc:Loc MetadataQualityMarkCorrect}");
        RequiredAttribute(button, "Command").Should().Be("{Binding MarkSelectedIssueCorrectCommand}");
        RequiredAttribute(button, "Focusable").Should().Be("True");
        RequiredAttribute(button, "AutomationProperties.Name").Should().Be("{loc:Loc MetadataQualityMarkCorrect}");
    }

    [Fact]
    public void RepairAuthorAction_IsCommandBoundAndKeyboardAccessible()
    {
        var document = XDocument.Load(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "MetadataQualityDashboardWindow.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var button = document
            .Descendants(presentation + "Button")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "RepairMissingAuthorButton");

        RequiredAttribute(button, "Content").Should().Be("{loc:Loc MetadataQualityChangeAuthor}");
        RequiredAttribute(button, "Command").Should().Be("{Binding RepairMissingAuthorCommand}");
        RequiredAttribute(button, "Focusable").Should().Be("True");
        RequiredAttribute(button, "AutomationProperties.Name").Should().Be("{loc:Loc MetadataQualityRepairMissingAuthor}");
    }

    [Fact]
    public void MainWindow_FilterPaneHasVisibleConstrainedSplitter()
    {
        var document = XDocument.Load(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "MainWindow.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var filterPaneColumn = document
            .Descendants(presentation + "ColumnDefinition")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "LibraryFilterPaneColumn");
        var splitter = document
            .Descendants(presentation + "GridSplitter")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "LibraryFilterPaneSplitter");

        RequiredAttribute(filterPaneColumn, "Width").Should().Be("220");
        RequiredAttribute(filterPaneColumn, "MinWidth").Should().Be("180");
        RequiredAttribute(filterPaneColumn, "MaxWidth").Should().Be("520");
        RequiredAttribute(splitter, "Style").Should().Be("{StaticResource VisibleVerticalGridSplitterStyle}");
        RequiredAttribute(splitter, "Focusable").Should().Be("True");
    }

    [Fact]
    public void MainWindow_MetadataCleanupOverlaySpansEveryContentColumn()
    {
        var document = XDocument.Load(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "MainWindow.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        var cleanupOverlay = document
            .Descendants(presentation + "Border")
            .Single(element => element
                .Descendants(presentation + "DataTrigger")
                .Any(trigger => (string?)trigger.Attribute("Binding") == "{Binding IsCleaningMetadata}"));

        RequiredAttribute(cleanupOverlay, "Grid.ColumnSpan").Should().Be("4");
    }

    [Fact]
    public void VerticalPaneSplitterStyle_HasVisibleGripAndInteractionFeedback()
    {
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "App.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var style = document
            .Descendants(presentation + "Style")
            .Single(element => (string?)element.Attribute(xaml + "Key") == "VisibleVerticalGridSplitterStyle");
        var grip = style
            .Descendants(presentation + "Border")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "Grip");
        var triggers = style.Descendants(presentation + "Trigger").ToArray();

        style.Elements(presentation + "Setter")
            .Should().Contain(element => (string?)element.Attribute("Property") == "Width" &&
                                         (string?)element.Attribute("Value") == "12");
        RequiredAttribute(grip, "Width").Should().Be("4");
        triggers.Should().Contain(element => (string?)element.Attribute("Property") == "IsMouseOver");
        triggers.Should().Contain(element => (string?)element.Attribute("Property") == "IsKeyboardFocused");
    }

    private static string RequiredAttribute(XElement element, XName name) =>
        element.Attribute(name)?.Value ?? throw new InvalidOperationException(
            $"Required attribute '{name}' is missing from '{element.Name.LocalName}'.");
}

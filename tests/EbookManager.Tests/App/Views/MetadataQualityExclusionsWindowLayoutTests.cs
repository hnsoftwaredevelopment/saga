using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Views;

public sealed class MetadataQualityExclusionsWindowLayoutTests
{
    [Fact]
    public void Window_supports_multi_selection_empty_state_and_accessible_restore_actions()
    {
        var document = XDocument.Load(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "MetadataQualityExclusionsWindow.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var grid = document.Descendants(presentation + "DataGrid")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "MetadataQualityExclusionsGrid");
        RequiredAttribute(grid, "ItemsSource").Should().Be("{Binding Rows}");
        RequiredAttribute(grid, "SelectionMode").Should().Be("Extended");
        RequiredAttribute(grid, "SelectionUnit").Should().Be("FullRow");
        RequiredAttribute(grid, "SelectionChanged").Should().Be("MetadataQualityExclusionsSelectionChanged");
        RequiredAttribute(grid, "IsReadOnly").Should().Be("True");
        grid.Descendants(presentation + "DataGridTextColumn")
            .Select(column => RequiredAttribute(column, "Binding"))
            .Should().Contain(
                "{Binding BookTitle}",
                "{Binding BookAuthors}",
                "{Binding Signal}",
                "{Binding CreatedAt, StringFormat=d}");

        var emptyState = document.Descendants(presentation + "TextBlock")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "MetadataQualityExclusionsEmptyState");
        emptyState.Descendants(presentation + "DataTrigger")
            .Should().ContainSingle(trigger =>
                (string?)trigger.Attribute("Binding") == "{Binding HasRows}" &&
                (string?)trigger.Attribute("Value") == "False");

        var restoreSelected = Button(document, xaml, "RestoreSelectedQualityExclusionsButton");
        RequiredAttribute(restoreSelected, "Command").Should().Be("{Binding RestoreSelectedCommand}");
        AssertAccessible(restoreSelected);

        var restoreAll = Button(document, xaml, "RestoreAllQualityExclusionsButton");
        RequiredAttribute(restoreAll, "Click").Should().Be("RestoreAllClicked");
        AssertAccessible(restoreAll);

        var close = Button(document, xaml, "CloseQualityExclusionsButton");
        RequiredAttribute(close, "IsCancel").Should().Be("True");
        AssertAccessible(close);
    }

    private static XElement Button(XDocument document, XNamespace xaml, string name)
    {
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        return document.Descendants(presentation + "Button")
            .Single(element => (string?)element.Attribute(xaml + "Name") == name);
    }

    private static void AssertAccessible(XElement button)
    {
        RequiredAttribute(button, "Focusable").Should().Be("True");
        RequiredAttribute(button, "AutomationProperties.Name").Should().NotBeNullOrWhiteSpace();
    }

    private static string RequiredAttribute(XElement element, XName name) =>
        element.Attribute(name)?.Value ?? throw new InvalidOperationException(
            $"Required attribute '{name}' is missing from '{element.Name.LocalName}'.");
}

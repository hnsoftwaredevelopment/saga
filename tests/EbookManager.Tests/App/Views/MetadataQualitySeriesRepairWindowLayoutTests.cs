using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Views;

public sealed class MetadataQualitySeriesRepairWindowLayoutTests
{
    [Fact]
    public void Window_shows_the_series_number_and_exposes_accessible_editable_suggestions()
    {
        var document = XDocument.Load(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "MetadataQualitySeriesRepairWindow.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var seriesNumber = document.Descendants(presentation + "TextBlock")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "SeriesNumberValue");
        RequiredAttribute(seriesNumber, "Text").Should().Be("{Binding SeriesNumber}");

        var input = document.Descendants(presentation + "ComboBox")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "SeriesInput");
        RequiredAttribute(input, "IsEditable").Should().Be("True");
        RequiredAttribute(input, "ItemsSource").Should().Be("{Binding Suggestions}");
        RequiredAttribute(input, "Text").Should().Contain("SeriesText");
        RequiredAttribute(input, "AutomationProperties.Name").Should().NotBeNullOrWhiteSpace();
        RequiredAttribute(input, "Loaded").Should().Be("SeriesInputLoaded");
        RequiredAttribute(input, "PreviewKeyDown").Should().Be("SeriesInputPreviewKeyDown");

        var save = Button(document, xaml, "SaveSeriesRepairButton");
        RequiredAttribute(save, "IsDefault").Should().Be("True");
        RequiredAttribute(save, "IsEnabled").Should().Be("{Binding CanSave}");
        RequiredAttribute(save, "Click").Should().Be("SaveClicked");
        AssertAccessible(save);

        var cancel = Button(document, xaml, "CancelSeriesRepairButton");
        RequiredAttribute(cancel, "IsCancel").Should().Be("True");
        AssertAccessible(cancel);
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

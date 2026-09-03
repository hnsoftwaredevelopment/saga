using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Views;

public sealed class MetadataQualityTitleAuthorRepairWindowLayoutTests
{
    [Fact]
    public void Window_shows_read_only_before_and_after_values_with_accessible_actions()
    {
        var document = XDocument.Load(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "MetadataQualityTitleAuthorRepairWindow.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        document.Descendants(presentation + "TextBox").Should().BeEmpty();
        BoundText(document, presentation, xaml, "CurrentTitleValue").Should().Be("{Binding CurrentTitle}");
        BoundText(document, presentation, xaml, "CurrentAuthorValue").Should().Be("{Binding CurrentAuthor}");
        BoundText(document, presentation, xaml, "NewTitleValue").Should().Be("{Binding NewTitle}");
        BoundText(document, presentation, xaml, "NewAuthorValue").Should().Be("{Binding NewAuthor}");

        var confirm = Button(document, presentation, xaml, "ConfirmTitleAuthorRepairButton");
        RequiredAttribute(confirm, "IsDefault").Should().Be("True");
        RequiredAttribute(confirm, "Click").Should().Be("ConfirmClicked");
        AssertAccessible(confirm);

        var cancel = Button(document, presentation, xaml, "CancelTitleAuthorRepairButton");
        RequiredAttribute(cancel, "IsCancel").Should().Be("True");
        AssertAccessible(cancel);
    }

    private static string BoundText(
        XDocument document,
        XNamespace presentation,
        XNamespace xaml,
        string name) =>
        RequiredAttribute(
            document.Descendants(presentation + "TextBlock")
                .Single(element => (string?)element.Attribute(xaml + "Name") == name),
            "Text");

    private static XElement Button(
        XDocument document,
        XNamespace presentation,
        XNamespace xaml,
        string name) =>
        document.Descendants(presentation + "Button")
            .Single(element => (string?)element.Attribute(xaml + "Name") == name);

    private static void AssertAccessible(XElement button)
    {
        RequiredAttribute(button, "Focusable").Should().Be("True");
        RequiredAttribute(button, "AutomationProperties.Name").Should().NotBeNullOrWhiteSpace();
    }

    private static string RequiredAttribute(XElement element, XName name) =>
        element.Attribute(name)?.Value ?? throw new InvalidOperationException(
            $"Required attribute '{name}' is missing from '{element.Name.LocalName}'.");
}

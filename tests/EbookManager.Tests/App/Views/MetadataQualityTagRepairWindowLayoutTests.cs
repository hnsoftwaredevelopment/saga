using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Views;

public sealed class MetadataQualityTagRepairWindowLayoutTests
{
    [Fact]
    public void Window_shows_current_and_editable_tags_with_accessible_actions()
    {
        var document = XDocument.Load(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "MetadataQualityTagRepairWindow.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var window = document.Root ?? throw new InvalidOperationException("Window root is missing.");
        RequiredAttribute(window, "ResizeMode").Should().Be("CanResizeWithGrip");

        var current = TextBox(document, presentation, xaml, "CurrentTagsOutput");
        RequiredAttribute(current, "Text").Should().Contain("CurrentTagsText");
        RequiredAttribute(current, "IsReadOnly").Should().Be("True");
        RequiredAttribute(current, "AcceptsReturn").Should().Be("True");
        AssertAccessible(current);

        var input = TextBox(document, presentation, xaml, "TagsInput");
        RequiredAttribute(input, "Text").Should().Contain("TagsText");
        RequiredAttribute(input, "AcceptsReturn").Should().Be("True");
        RequiredAttribute(input, "Loaded").Should().Be("TagsInputLoaded");
        AssertAccessible(input);

        var save = Button(document, presentation, xaml, "SaveTagRepairButton");
        RequiredAttribute(save, "IsDefault").Should().Be("True");
        RequiredAttribute(save, "IsEnabled").Should().Be("{Binding CanSave}");
        RequiredAttribute(save, "Click").Should().Be("SaveClicked");
        AssertAccessible(save);

        var cancel = Button(document, presentation, xaml, "CancelTagRepairButton");
        RequiredAttribute(cancel, "IsCancel").Should().Be("True");
        AssertAccessible(cancel);
    }

    [Fact]
    public void Dashboard_exposes_tag_cleanup_only_for_the_messy_tag_signal()
    {
        var document = XDocument.Load(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "MetadataQualityDashboardWindow.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var button = Button(document, presentation, xaml, "RepairMessyTagsButton");

        RequiredAttribute(button, "Content").Should().Be("{loc:Loc MetadataQualityCleanTags}");
        RequiredAttribute(button, "Command").Should().Be("{Binding RepairMessyTagsCommand}");
        AssertAccessible(button);
        button.Descendants(presentation + "DataTrigger").Should().ContainSingle(trigger =>
            (string?)trigger.Attribute("Binding") == "{Binding SelectedIssue.SignalKey}" &&
            (string?)trigger.Attribute("Value") == "messy-tags" &&
            trigger.Elements(presentation + "Setter").Any(setter =>
                (string?)setter.Attribute("Property") == "Visibility" &&
                (string?)setter.Attribute("Value") == "Visible"));
    }

    private static XElement TextBox(
        XDocument document,
        XNamespace presentation,
        XNamespace xaml,
        string name) =>
        document.Descendants(presentation + "TextBox")
            .Single(element => (string?)element.Attribute(xaml + "Name") == name);

    private static XElement Button(
        XDocument document,
        XNamespace presentation,
        XNamespace xaml,
        string name) =>
        document.Descendants(presentation + "Button")
            .Single(element => (string?)element.Attribute(xaml + "Name") == name);

    private static void AssertAccessible(XElement element)
    {
        RequiredAttribute(element, "Focusable").Should().Be("True");
        RequiredAttribute(element, "AutomationProperties.Name").Should().NotBeNullOrWhiteSpace();
    }

    private static string RequiredAttribute(XElement element, XName name) =>
        element.Attribute(name)?.Value ?? throw new InvalidOperationException(
            $"Required attribute '{name}' is missing from '{element.Name.LocalName}'.");
}

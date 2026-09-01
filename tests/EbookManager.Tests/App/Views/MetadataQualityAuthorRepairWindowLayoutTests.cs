using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Views;

public sealed class MetadataQualityAuthorRepairWindowLayoutTests
{
    [Fact]
    public void Window_exposes_editable_suggestions_validation_and_accessible_actions()
    {
        var document = XDocument.Load(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "MetadataQualityAuthorRepairWindow.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var window = document.Root!;
        int.Parse(RequiredAttribute(window, "MinWidth")).Should().BeGreaterThanOrEqualTo(420);
        RequiredAttribute(window, "WindowStartupLocation").Should().Be("CenterOwner");

        var authorInput = document.Descendants(presentation + "ComboBox")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "AuthorInput");
        RequiredAttribute(authorInput, "IsEditable").Should().Be("True");
        RequiredAttribute(authorInput, "ItemsSource").Should().Be("{Binding Suggestions}");
        RequiredAttribute(authorInput, "Text").Should().Contain("AuthorText");
        RequiredAttribute(authorInput, "AutomationProperties.Name").Should().NotBeNullOrWhiteSpace();
        RequiredAttribute(authorInput, "Loaded").Should().Be("AuthorInputLoaded");
        RequiredAttribute(authorInput, "PreviewKeyDown").Should().Be("AuthorInputPreviewKeyDown");

        var save = Button(document, xaml, "SaveAuthorRepairButton");
        RequiredAttribute(save, "IsDefault").Should().Be("True");
        RequiredAttribute(save, "IsEnabled").Should().Be("{Binding CanSave}");
        RequiredAttribute(save, "Click").Should().Be("SaveClicked");
        AssertAccessible(save);

        var cancel = Button(document, xaml, "CancelAuthorRepairButton");
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

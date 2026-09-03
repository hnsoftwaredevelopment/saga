using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Views;

public sealed class MetadataQualityLanguageRepairWindowLayoutTests
{
    [Fact]
    public void Window_exposes_a_valid_language_choice_and_accessible_actions()
    {
        var document = XDocument.Load(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "MetadataQualityLanguageRepairWindow.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var window = document.Root!;
        int.Parse(RequiredAttribute(window, "MinWidth")).Should().BeGreaterThanOrEqualTo(420);
        RequiredAttribute(window, "WindowStartupLocation").Should().Be("CenterOwner");

        var languageInput = document.Descendants(presentation + "ComboBox")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "LanguageInput");
        RequiredAttribute(languageInput, "ItemsSource").Should().Be("{Binding Languages}");
        RequiredAttribute(languageInput, "SelectedItem").Should().Contain("SelectedLanguage");
        RequiredAttribute(languageInput, "DisplayMemberPath").Should().Be("DisplayText");
        RequiredAttribute(languageInput, "IsTextSearchEnabled").Should().Be("True");
        RequiredAttribute(languageInput, "AutomationProperties.Name").Should().NotBeNullOrWhiteSpace();

        var save = Button(document, xaml, "SaveLanguageRepairButton");
        RequiredAttribute(save, "IsDefault").Should().Be("True");
        RequiredAttribute(save, "IsEnabled").Should().Be("{Binding CanSave}");
        RequiredAttribute(save, "Click").Should().Be("SaveClicked");
        AssertAccessible(save);

        var cancel = Button(document, xaml, "CancelLanguageRepairButton");
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

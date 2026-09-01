using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Views;

public sealed class SettingsWindowQualityExclusionsLayoutTests
{
    [Fact]
    public void Duplicates_section_exposes_accessible_quality_exclusions_management_action()
    {
        var document = XDocument.Load(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "SettingsWindow.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var button = document.Descendants(presentation + "Button")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "ManageMetadataQualityExclusionsButton");

        RequiredAttribute(button, "Content").Should().Be("{loc:Loc ManageMetadataQualityExclusions}");
        RequiredAttribute(button, "Command").Should().Be(
            "{Binding LibraryViewModel.ShowMetadataQualityExclusionsCommand, ElementName=SettingsRoot}");
        RequiredAttribute(button, "Focusable").Should().Be("True");
        RequiredAttribute(button, "AutomationProperties.Name").Should().Be("{loc:Loc ManageMetadataQualityExclusions}");
    }

    private static string RequiredAttribute(XElement element, XName name) =>
        element.Attribute(name)?.Value ?? throw new InvalidOperationException(
            $"Required attribute '{name}' is missing from '{element.Name.LocalName}'.");
}

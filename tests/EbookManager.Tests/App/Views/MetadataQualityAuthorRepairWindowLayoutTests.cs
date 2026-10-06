using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Views;

public sealed class MetadataQualityAuthorRepairWindowLayoutTests
{
    [Fact]
    public void Arrow_down_opens_known_authors_before_text_is_entered()
    {
        var source = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "MetadataQualityAuthorRepairWindow.xaml.cs"));

        source.Should().Contain("e.Key == Key.Down &&");
        source.Should().Contain("AuthorSuggestions.Items.Count > 0)");
        source.Should().NotContain("AuthorSuggestionsPopup.IsOpen &&");
        source.Should().Contain("AuthorSuggestionsPopup.IsOpen = true;");
    }

    [Fact]
    public void Window_keeps_typed_text_separate_from_the_changing_suggestion_list()
    {
        var document = XDocument.Load(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "MetadataQualityAuthorRepairWindow.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var window = document.Root!;
        int.Parse(RequiredAttribute(window, "MinWidth")).Should().BeGreaterThanOrEqualTo(420);
        RequiredAttribute(window, "WindowStartupLocation").Should().Be("CenterOwner");

        var authorInput = document.Descendants(presentation + "TextBox")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "AuthorInput");
        RequiredAttribute(authorInput, "Text").Should().Contain("AuthorText");
        RequiredAttribute(authorInput, "AutomationProperties.Name").Should().NotBeNullOrWhiteSpace();
        RequiredAttribute(authorInput, "Loaded").Should().Be("AuthorInputLoaded");
        RequiredAttribute(authorInput, "TextChanged").Should().Be("AuthorInputTextChanged");
        RequiredAttribute(authorInput, "PreviewKeyDown").Should().Be("AuthorInputPreviewKeyDown");

        var popup = document.Descendants(presentation + "Popup")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "AuthorSuggestionsPopup");
        RequiredAttribute(popup, "PlacementTarget").Should().Be("{Binding ElementName=AuthorInput}");

        var suggestions = popup.Descendants(presentation + "ListBox")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "AuthorSuggestions");
        RequiredAttribute(suggestions, "ItemsSource").Should().Be("{Binding Suggestions}");
        RequiredAttribute(suggestions, "VirtualizingStackPanel.IsVirtualizing").Should().Be("True");
        RequiredAttribute(suggestions, "VirtualizingStackPanel.VirtualizationMode").Should().Be("Recycling");
        RequiredAttribute(suggestions, "PreviewKeyDown").Should().Be("AuthorSuggestionsPreviewKeyDown");
        RequiredAttribute(suggestions, "PreviewMouseLeftButtonUp")
            .Should().Be("AuthorSuggestionsMouseLeftButtonUp");

        var contextLabel = document.Descendants(presentation + "TextBlock")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "BookContextLabel");
        RequiredAttribute(contextLabel, "Text").Should().Be("{Binding ContextLabel}");
        var contextText = document.Descendants(presentation + "TextBlock")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "BookContextText");
        RequiredAttribute(contextText, "Text").Should().Be("{Binding ContextText}");

        var save = Button(document, xaml, "SaveAuthorRepairButton");
        RequiredAttribute(save, "Content").Should().Be("{Binding SaveButtonText}");
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

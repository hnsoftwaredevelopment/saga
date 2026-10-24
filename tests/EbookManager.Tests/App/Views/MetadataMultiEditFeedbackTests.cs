using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Views;

public sealed class MetadataMultiEditFeedbackTests
{
    [Fact]
    public void Tags_can_be_edited_before_selecting_the_update_checkbox()
    {
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "MetadataMultiEditWindow.xaml"));
        XNamespace p = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var tags = document.Descendants(p + "TextBox").Single(element => (string?)element.Attribute("Text") == "{Binding TagsText, UpdateSourceTrigger=PropertyChanged}");
        tags.AncestorsAndSelf().Should().NotContain(element => (string?)element.Attribute("IsEnabled") == "{Binding UpdateTags}");
    }

    [Fact]
    public void MultiEdit_feedback_is_bounded_virtualized_and_keyboard_expandable()
    {
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "MetadataMultiEditFeedbackView.xaml"));
        XNamespace p = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        document.Descendants(p + "Expander").Should().ContainSingle();
        var list = document.Descendants(p + "ListBox").Single();
        double.Parse(list.Attribute("MaxHeight")!.Value).Should().BeInRange(100, 200);
        list.Attribute("VirtualizingPanel.VirtualizationMode")!.Value.Should().Be("Recycling");
        list.Attribute("AutomationProperties.Name").Should().NotBeNull();
        document.Descendants(p + "DataTrigger").Should().Contain(element => (string?)element.Attribute("Binding") == "{Binding MultiEditIsError}");
    }

    [Fact]
    public void MultiEdit_feedback_has_all_translations_with_matching_format_parameters()
    {
        var english = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "Strings", "AppResources.resx"));
        var keys = english.Descendants("data").Where(element => ((string)element.Attribute("name")!).StartsWith("MultiEdit", StringComparison.Ordinal)).ToArray();
        keys.Should().HaveCount(15);
        foreach (var culture in new[] { "nl", "de", "fr", "es", "it" })
        {
            var translated = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "Strings", $"AppResources.{culture}.resx"));
            foreach (var key in keys)
            {
                var value = translated.Descendants("data").Single(element => (string?)element.Attribute("name") == (string?)key.Attribute("name")).Element("value")!.Value;
                value.Should().NotBeNullOrWhiteSpace();
                foreach (var index in new[] { 0, 1, 2 })
                    value.Contains("{" + index + "}").Should().Be(key.Element("value")!.Value.Contains("{" + index + "}"));
            }
        }
    }
}

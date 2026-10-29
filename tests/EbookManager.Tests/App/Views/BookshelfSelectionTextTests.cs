using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Views;

public sealed class BookshelfSelectionTextTests
{
    [Theory]
    [InlineData("")]
    [InlineData(".nl")]
    [InlineData(".de")]
    [InlineData(".fr")]
    [InlineData(".es")]
    [InlineData(".it")]
    public void Bookshelf_selection_explanation_and_existing_action_labels_are_localized(string language)
    {
        var resources = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "Strings", $"AppResources{language}.resx"));
        foreach (var key in new[] { "BookshelfSelectionHelp", "MetadataMultiEditSelectedBooks", "MetadataMultiEditTitle" })
            resources.Descendants("data").Single(row => (string?)row.Attribute("name") == key).Element("value")!.Value.Should().NotBeNullOrWhiteSpace().And.NotBe(key);
    }
}

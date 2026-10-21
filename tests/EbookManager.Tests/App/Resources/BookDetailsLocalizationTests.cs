using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Resources;

public sealed class BookDetailsLocalizationTests
{
    [Theory]
    [InlineData("AppResources.resx")]
    [InlineData("AppResources.nl.resx")]
    [InlineData("AppResources.de.resx")]
    [InlineData("AppResources.fr.resx")]
    [InlineData("AppResources.es.resx")]
    [InlineData("AppResources.it.resx")]
    public void Supported_resources_name_the_title_author_swap_action(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestAssets", "Strings", fileName);
        var document = XDocument.Load(path);
        var value = document.Root!
            .Elements("data")
            .Single(element => (string?)element.Attribute("name") == "SwapTitleAndAuthorsTooltip")
            .Element("value")!
            .Value;

        value.Should().NotBeNullOrWhiteSpace();
    }
}

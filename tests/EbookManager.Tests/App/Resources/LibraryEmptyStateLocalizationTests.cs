using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Resources;

public sealed class LibraryEmptyStateLocalizationTests
{
    [Theory]
    [InlineData("AppResources.resx")]
    [InlineData("AppResources.nl.resx")]
    [InlineData("AppResources.de.resx")]
    [InlineData("AppResources.fr.resx")]
    [InlineData("AppResources.es.resx")]
    [InlineData("AppResources.it.resx")]
    public void Supported_resources_describe_an_empty_filtered_result(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestAssets", "Strings", fileName);
        var document = XDocument.Load(path);
        var message = document.Root!
            .Elements("data")
            .Single(element => (string?)element.Attribute("name") == "EmptyStateNoMatchingBooks")
            .Element("value")?.Value;

        message.Should().NotBeNullOrWhiteSpace();
    }
}

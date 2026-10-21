using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Resources;

public sealed class DuplicateMergeLocalizationTests
{
    [Theory]
    [InlineData("AppResources.resx")]
    [InlineData("AppResources.nl.resx")]
    [InlineData("AppResources.de.resx")]
    [InlineData("AppResources.fr.resx")]
    [InlineData("AppResources.es.resx")]
    [InlineData("AppResources.it.resx")]
    public void Supported_resources_contain_sidecar_warning_texts(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestAssets", "Strings", fileName);
        var document = XDocument.Load(path);
        var values = document.Root!
            .Elements("data")
            .ToDictionary(
                element => (string)element.Attribute("name")!,
                element => element.Element("value")?.Value ?? string.Empty,
                StringComparer.Ordinal);

        values["DuplicateMergeSidecarWarningTitle"].Should().NotBeNullOrWhiteSpace();
        values["DuplicateMergeSidecarWarningMessage"].Should().Contain("metadata.json");
    }
}

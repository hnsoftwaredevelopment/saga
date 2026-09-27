using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Resources;

public sealed class LibraryStorageLocalizationTests
{
    [Theory]
    [InlineData("AppResources.resx")]
    [InlineData("AppResources.nl.resx")]
    [InlineData("AppResources.de.resx")]
    [InlineData("AppResources.fr.resx")]
    [InlineData("AppResources.es.resx")]
    [InlineData("AppResources.it.resx")]
    public void Supported_resources_contain_storage_migration_texts(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestAssets", "Strings", fileName);
        var document = XDocument.Load(path);
        var values = document.Root!
            .Elements("data")
            .ToDictionary(
                element => (string)element.Attribute("name")!,
                element => element.Element("value")?.Value ?? string.Empty,
                StringComparer.Ordinal);

        values["StorageMigrationStatus"].Should().NotBeNullOrWhiteSpace();
        values["StorageMigrationFailedTitle"].Should().NotBeNullOrWhiteSpace();
        values["StorageMigrationFailedMessage"].Should().Contain("{0}");
    }
}

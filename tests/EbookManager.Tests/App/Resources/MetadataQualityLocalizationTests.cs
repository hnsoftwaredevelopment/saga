using System.Xml.Linq;
using EbookManager.Domain.Metadata;
using FluentAssertions;

namespace EbookManager.Tests.App.Resources;

public sealed class MetadataQualityLocalizationTests
{
    private static readonly string[] RequiredFeatureKeys =
    [
        "MetadataQualityMarkCorrect",
        "MetadataQualityMarkCorrectFailed",
        "MetadataQualityExclusionsTitle",
        "MetadataQualityExclusionsDescription",
        "MetadataQualityExclusionsCountSuffix",
        "RestoreSelectedQualityExclusions",
        "RestoreAllQualityExclusions",
        "MetadataQualityExclusionsNone",
        "MetadataQualitySignal",
        "RestoreAllQualityExclusionsConfirmationMessage",
        "RestoreAllQualityExclusionsConfirmationTitle",
        "MetadataQualityExclusionsSettingsTitle",
        "MetadataQualityExclusionsSettingsDescription",
        "ManageMetadataQualityExclusions",
        "MetadataQualityAuthorRepairTitle",
        "MetadataQualityAuthorRepairDescription",
        "MetadataQualityAuthorRepairAuthorInput",
        "MetadataQualityAuthorRepairAuthorHelp",
        "MetadataQualityAuthorRepairSave",
        "MetadataQualityRepair",
        "MetadataQualityChangeAuthor",
        "MetadataQualityRepairMissingAuthor",
        "MetadataQualityAuthorRepairFailed",
        "MetadataQualityAuthorRepairWriteBackWarning",
        "MetadataQualityAuthorRepairNotNeeded"
    ];

    [Theory]
    [InlineData("AppResources.resx")]
    [InlineData("AppResources.nl.resx")]
    [InlineData("AppResources.de.resx")]
    [InlineData("AppResources.fr.resx")]
    [InlineData("AppResources.es.resx")]
    [InlineData("AppResources.it.resx")]
    public void Supported_resources_contain_understandable_quality_workflow_texts(string fileName)
    {
        var values = LoadResourceValues(fileName);

        foreach (var key in RequiredFeatureKeys)
        {
            values.Should().ContainKey(key);
            values[key].Should().NotBeNullOrWhiteSpace();
            values[key].Should().NotBe(key);
        }

        values.Values.Should().NotContain(value => MetadataQualitySignalKeys.All.Contains(value));
    }

    private static IReadOnlyDictionary<string, string> LoadResourceValues(string fileName)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "TestAssets",
            "Strings",
            fileName);
        var document = XDocument.Load(path);
        return document.Root!
            .Elements("data")
            .ToDictionary(
                element => (string)element.Attribute("name")!,
                element => element.Element("value")?.Value ?? string.Empty,
                StringComparer.Ordinal);
    }
}

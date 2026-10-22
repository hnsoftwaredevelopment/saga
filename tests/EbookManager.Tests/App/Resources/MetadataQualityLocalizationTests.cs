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
        "MetadataQualityMarkCorrectBulkConfirmationTitle",
        "MetadataQualityMarkCorrectBulkConfirmationMessage",
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
        "SettingsQualitySection",
        "MetadataQualityAuthorRepairTitle",
        "MetadataQualityAuthorRepairDescription",
        "MetadataQualityAuthorRepairAuthorInput",
        "MetadataQualityAuthorRepairAuthorHelp",
        "MetadataQualityAuthorRepairSave",
        "MetadataQualityAuthorRepairBulkBookContext",
        "MetadataQualityAuthorRepairBulkSave",
        "MetadataQualityAuthorRepairBulkSucceeded",
        "MetadataQualityAuthorRepairBulkResult",
        "MetadataQualityRepair",
        "MetadataQualityChangeAuthor",
        "MetadataQualityRepairMissingAuthor",
        "MetadataQualityAuthorRepairFailed",
        "MetadataQualityAuthorRepairWriteBackWarning",
        "MetadataQualityAuthorRepairNotNeeded",
        "MetadataQualityChangeLanguage",
        "MetadataQualityRepairUnknownLanguage",
        "MetadataQualityLanguageRepairTitle",
        "MetadataQualityLanguageRepairDescription",
        "MetadataQualityLanguageRepairInput",
        "MetadataQualityLanguageRepairHelp",
        "MetadataQualityLanguageRepairSave",
        "MetadataQualityLanguageRepairBulkBookContext",
        "MetadataQualityLanguageRepairBulkSave",
        "MetadataQualityLanguageRepairBulkSucceeded",
        "MetadataQualityLanguageRepairBulkResult",
        "MetadataQualityLanguageRepairFailed",
        "MetadataQualityLanguageRepairWriteBackWarning",
        "MetadataQualityLanguageRepairNotNeeded",
        "MetadataQualityChangeSeries",
        "MetadataQualityRepairMissingSeries",
        "MetadataQualitySeriesRepairTitle",
        "MetadataQualitySeriesRepairDescription",
        "MetadataQualitySeriesRepairInput",
        "MetadataQualitySeriesRepairHelp",
        "MetadataQualitySeriesRepairSave",
        "MetadataQualitySeriesRepairBulkBookContext",
        "MetadataQualitySeriesRepairBulkSeriesNumbersLabel",
        "MetadataQualitySeriesRepairBulkSeriesNumbersHelp",
        "MetadataQualitySeriesRepairBulkSave",
        "MetadataQualitySeriesRepairBulkSucceeded",
        "MetadataQualitySeriesRepairBulkResult",
        "MetadataQualitySeriesRepairFailed",
        "MetadataQualitySeriesRepairWriteBackWarning",
        "MetadataQualitySeriesRepairNotNeeded",
        "MetadataQualitySwapTitleAuthor",
        "MetadataQualityTitleAuthorRepairTitle",
        "MetadataQualityTitleAuthorRepairDescription",
        "MetadataQualityTitleAuthorRepairCurrent",
        "MetadataQualityTitleAuthorRepairAfter",
        "MetadataQualityTitleAuthorRepairConfirm",
        "MetadataQualityTitleAuthorRepairFailed",
        "MetadataQualityTitleAuthorRepairWriteBackWarning",
        "MetadataQualityTitleAuthorRepairNotNeeded",
        "MetadataQualitySearchCover",
        "MetadataQualityCoverSearchTitle",
        "MetadataQualityCoverSearchDescription",
        "MetadataQualityUseCover",
        "MetadataQualityCoverSearchResults",
        "MetadataQualityCoverSearchLoading",
        "MetadataQualityCoverSearchNoResults",
        "MetadataQualityCoverSearchFailed",
        "MetadataQualityCoverDownloadFailed",
        "MetadataQualityCoverRepairFailed",
        "MetadataQualityCoverRepairWriteBackWarning",
        "MetadataQualityCoverRepairNotNeeded",
        "ChangeCover",
        "MetadataQualityCleanTags",
        "MetadataQualityTagRepairTitle",
        "MetadataQualityTagRepairDescription",
        "MetadataQualityCurrentTags",
        "MetadataQualityNewTags",
        "MetadataQualityTagRepairHelp",
        "MetadataQualityChangeTags",
        "MetadataQualityTagRepairFailed",
        "MetadataQualityTagRepairWriteBackWarning",
        "MetadataQualityTagRepairNotNeeded",
        "MetadataQualityTagRepairBulkContext",
        "MetadataQualityTagRepairBulkSave",
        "MetadataQualityTagRepairBulkHelp",
        "MetadataQualityTagRepairBulkPreview",
        "MetadataQualityTagRepairEmptyTags",
        "MetadataQualityTagRepairBulkSucceeded",
        "MetadataQualityTagRepairBulkResult"
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
        values["MetadataQualityAuthorRepairBulkBookContext"].Should().Contain("{0}");
        values["MetadataQualityAuthorRepairBulkSave"].Should().Contain("{0}");
        values["MetadataQualityAuthorRepairBulkSucceeded"].Should().Contain("{0}");
        values["MetadataQualityLanguageRepairBulkBookContext"].Should().Contain("{0}");
        values["MetadataQualityLanguageRepairBulkSave"].Should().Contain("{0}");
        values["MetadataQualityLanguageRepairBulkSucceeded"].Should().Contain("{0}");
        values["MetadataQualitySeriesRepairBulkBookContext"].Should().Contain("{0}");
        values["MetadataQualitySeriesRepairBulkSave"].Should().Contain("{0}");
        values["MetadataQualitySeriesRepairBulkSucceeded"].Should().Contain("{0}");
        values["MetadataQualityTagRepairBulkContext"].Should().Contain("{0}");
        values["MetadataQualityTagRepairBulkSave"].Should().Contain("{0}");
        values["MetadataQualityTagRepairBulkSucceeded"].Should().Contain("{0}");
        foreach (var placeholder in Enumerable.Range(0, 5).Select(index => $"{{{index}}}"))
        {
            values["MetadataQualityAuthorRepairBulkResult"].Should().Contain(placeholder);
            values["MetadataQualityLanguageRepairBulkResult"].Should().Contain(placeholder);
            values["MetadataQualitySeriesRepairBulkResult"].Should().Contain(placeholder);
            values["MetadataQualityTagRepairBulkResult"].Should().Contain(placeholder);
        }
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

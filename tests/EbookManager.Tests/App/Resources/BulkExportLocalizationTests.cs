using System.Globalization;
using System.Xml.Linq;
using EbookManager.Application.Books;
using FluentAssertions;

namespace EbookManager.Tests.App.Resources;

public sealed class BulkExportLocalizationTests
{
    [Theory]
    [InlineData("")][InlineData(".nl")][InlineData(".de")][InlineData(".fr")][InlineData(".es")][InlineData(".it")]
    public void Export_texts_and_all_outcomes_exist_with_usable_placeholders(string language)
    {
        var resource = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "Strings", $"AppResources{language}.resx"))
            .Root!.Elements("data").ToDictionary(element => (string)element.Attribute("name")!, element => element.Element("value")!.Value);
        var keys = Enum.GetNames<BulkExportStatus>().Select(status => "BulkExport" + status).Concat(new[]
        {
            "BulkExportTitle", "BulkExportHelp", "BulkExportFormat", "BulkExportStart", "BulkExportOutcome", "BulkExportDestination",
            "BulkExportPreparing", "BulkExportAvailability", "BulkExportFileCount", "BulkExportProgress", "BulkExportSummary", "BulkExportSuccess",
            "BulkExportStopped", "BulkExportSkipped", "BulkExportCancelled", "BulkExportCancelling", "BulkExportLoadFailed", "BulkExportUnexpected", "BulkExportFolderFailed"
        });
        var required = new Dictionary<string, string[]>
        {
            ["BulkExportAvailability"] = ["{0}", "{1}", "{2}"],
            ["BulkExportFileCount"] = ["{0}"],
            ["BulkExportProgress"] = ["{0}", "{1}", "{2}"],
            ["BulkExportSummary"] = ["{0}", "{1}", "{2}"],
            ["BulkExportSuccess"] = ["{0}"],
            ["BulkExportSkipped"] = ["{0}"],
            ["BulkExportStopped"] = ["{0}"]
        };
        foreach (var key in keys)
        {
            resource[key].Should().NotBeNullOrWhiteSpace().And.NotBe(key);
            if (required.TryGetValue(key, out var placeholders)) resource[key].Should().ContainAll(placeholders);
            foreach (var count in new[] { 0, 1, 2 })
                string.Format(CultureInfo.InvariantCulture, resource[key], count, count, "Title")
                    .Should().NotContainAny("{0}", "{1}", "{2}");
        }
    }
}

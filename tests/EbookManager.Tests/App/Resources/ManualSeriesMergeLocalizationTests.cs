using System.Globalization;
using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Resources;

public sealed class ManualSeriesMergeLocalizationTests
{
    [Theory]
    [InlineData("")][InlineData(".nl")][InlineData(".de")][InlineData(".fr")][InlineData(".es")][InlineData(".it")]
    public void Manual_series_merge_actions_help_and_conflicts_are_localized(string language)
    {
        var resource = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "Strings", $"AppResources{language}.resx"))
            .Root!.Elements("data").ToDictionary(element => (string)element.Attribute("name")!, element => element.Element("value")!.Value);
        foreach (var suffix in new[] { "Action", "Selected", "Help", "NewName", "NameExists" })
        {
            var key = "SeriesManualMerge" + suffix;
            resource[key].Should().NotBeNullOrWhiteSpace().And.NotBe(key);
            if (suffix == "Selected") resource[key].Should().Contain("{0}");
            foreach (var count in new[] { 0, 1, 2 }) string.Format(CultureInfo.InvariantCulture, resource[key], count).Should().NotContain("{0}");
        }
    }
}

using System.Globalization;
using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Resources;

public sealed class SeriesNumbersLocalizationTests
{
    [Theory]
    [InlineData("")][InlineData(".nl")][InlineData(".de")][InlineData(".fr")][InlineData(".es")][InlineData(".it")]
    public void Series_split_explains_the_selection_and_distinguishes_saving_from_refresh_failure(string language)
    {
        var resource = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "Strings", $"AppResources{language}.resx"))
            .Root!.Elements("data").ToDictionary(element => (string)element.Attribute("name")!, element => element.Element("value")!.Value);
        foreach (var suffix in new[] { "Action", "Explanation", "Working", "Saved", "SavedWithWarnings", "Conflict", "Failed", "RefreshFailed" })
        {
            var key = "SeriesSplit" + suffix;
            resource[key].Should().NotBeNullOrWhiteSpace().And.NotBe(key);
            if (suffix is "Explanation" or "Saved" or "SavedWithWarnings") resource[key].Should().Contain("{0}");
            if (suffix is "Explanation" or "SavedWithWarnings") resource[key].Should().Contain("{1}");
            string.Format(CultureInfo.InvariantCulture, resource[key], 1, 2).Should().NotContain("{0}").And.NotContain("{1}");
        }
    }

    [Theory]
    [InlineData("")][InlineData(".nl")][InlineData(".de")][InlineData(".fr")][InlineData(".es")][InlineData(".it")]
    public void Series_number_proposal_has_localized_actions_validation_and_complete_count_placeholders(string language)
    {
        var resource = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "Strings", $"AppResources{language}.resx"))
            .Root!.Elements("data").ToDictionary(element => (string)element.Attribute("name")!, element => element.Element("value")!.Value);
        foreach (var suffix in new[] { "Assign", "Help", "Start", "Up", "Down", "Current", "Proposed", "Invalid", "Saved", "Outside", "Warnings", "Duplicates", "Conflict", "Working", "Failed", "RefreshFailed" })
        {
            var key = "SeriesNumbers" + suffix;
            resource[key].Should().NotBeNullOrWhiteSpace().And.NotBe(key);
            if (suffix is "Saved" or "Warnings" or "Duplicates") resource[key].Should().Contain("{0}");
            foreach (var count in new[] { 0, 1, 2 }) string.Format(CultureInfo.InvariantCulture, resource[key], count).Should().NotContain("{0}");
        }
    }
}

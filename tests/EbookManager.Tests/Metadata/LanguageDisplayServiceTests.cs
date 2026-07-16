using EbookManager.Application.Metadata;
using FluentAssertions;
using System.Globalization;

namespace EbookManager.Tests.Metadata;

public sealed class LanguageDisplayServiceTests
{
    [Theory]
    [InlineData("eng", "en")]
    [InlineData("en-US", "en")]
    [InlineData("nl-NL", "nl")]
    [InlineData("nl", "nl")]
    [InlineData("Nederlands", "nl")]
    [InlineData("Dutch", "nl")]
    [InlineData("Niederländisch", "nl")]
    [InlineData("néerlandais", "nl")]
    [InlineData("English", "en")]
    [InlineData("Engels", "en")]
    [InlineData("Deutsch", "de")]
    [InlineData("Français", "fr")]
    [InlineData("Español", "es")]
    [InlineData("Italiano", "it")]
    [InlineData("lv", "lv")]
    [InlineData("Latin", "Latin")]
    public void FilterKey_normalizes_common_language_values(string value, string expected)
    {
        LanguageDisplayService.FilterKey(value).Should().Be(expected);
    }

    [Fact]
    public void DisplayName_uses_current_ui_culture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");

            LanguageDisplayService.DisplayName("eng").Should().Be("Engels");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void DisplayName_returns_original_value_when_language_is_unknown()
    {
        LanguageDisplayService.DisplayName("fictional-language").Should().Be("fictional-language");
    }
}

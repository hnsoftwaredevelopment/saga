using EbookManager.Application.Metadata;
using EbookManager.Domain.Settings;
using FluentAssertions;

namespace EbookManager.Tests.Metadata;

public sealed class AuthorSortKeyBuilderTests
{
    [Theory]
    [InlineData("Karin Slaughter", "Karin Slaughter")]
    [InlineData("Slaughter, Karin", "Slaughter, Karin")]
    public void BuildSortKey_keeps_display_name_when_strategy_is_display_name(string author, string expected)
    {
        AuthorSortKeyBuilder.BuildSortKey(author, AuthorSortStrategy.DisplayName).Should().Be(expected);
    }

    [Theory]
    [InlineData("Karin Slaughter", "Slaughter, Karin")]
    [InlineData("J.R.R. Tolkien", "Tolkien, J.R.R.")]
    [InlineData("Slaughter, Karin", "Slaughter, Karin")]
    [InlineData("Unknown", "Unknown")]
    public void BuildSortKey_moves_last_token_first_when_strategy_is_last_name_first(string author, string expected)
    {
        AuthorSortKeyBuilder.BuildSortKey(author, AuthorSortStrategy.LastNameFirst).Should().Be(expected);
    }

    [Theory]
    [InlineData("Vincent van Gogh", "van Gogh, Vincent")]
    [InlineData("Peter van de Velde", "van de Velde, Peter")]
    [InlineData("Karin Slaughter", "Slaughter, Karin")]
    public void BuildSortKey_keeps_dutch_prefixes_with_last_name_when_strategy_uses_dutch_prefixes(string author, string expected)
    {
        AuthorSortKeyBuilder.BuildSortKey(author, AuthorSortStrategy.LastNameFirstDutchPrefixes).Should().Be(expected);
    }

    [Fact]
    public void BuildSortKey_uses_first_author_from_display_list()
    {
        AuthorSortKeyBuilder.BuildSortKey("Karin Slaughter; Lee Child", AuthorSortStrategy.LastNameFirst)
            .Should()
            .Be("Slaughter, Karin");
    }
}

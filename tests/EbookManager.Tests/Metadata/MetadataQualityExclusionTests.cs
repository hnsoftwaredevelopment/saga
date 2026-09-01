using EbookManager.Domain.Metadata;
using FluentAssertions;

namespace EbookManager.Tests.Metadata;

public sealed class MetadataQualityExclusionTests
{
    [Fact]
    public void Known_signal_keys_match_the_persistent_contract()
    {
        MetadataQualitySignalKeys.All.Should().Equal(
            "missing-author",
            "unknown-language",
            "missing-cover",
            "series-number-without-series",
            "possible-title-author-swap",
            "messy-tags");
        MetadataQualitySignalKeys.All.Should().OnlyHaveUniqueItems();
        MetadataQualitySignalKeys.All.Should().OnlyContain(key => !string.IsNullOrWhiteSpace(key));
    }

    [Fact]
    public void Exclusion_key_identity_includes_book_and_signal()
    {
        var bookId = Guid.NewGuid();
        var key = new MetadataQualityExclusionKey(bookId, MetadataQualitySignalKeys.MissingAuthor);

        key.Should().Be(new MetadataQualityExclusionKey(bookId, MetadataQualitySignalKeys.MissingAuthor));
        key.Should().NotBe(new MetadataQualityExclusionKey(bookId, MetadataQualitySignalKeys.MissingCover));
        key.Should().NotBe(new MetadataQualityExclusionKey(Guid.NewGuid(), MetadataQualitySignalKeys.MissingAuthor));
    }

    [Fact]
    public void Exclusion_key_rejects_an_empty_book_id()
    {
        var act = () => new MetadataQualityExclusionKey(Guid.Empty, MetadataQualitySignalKeys.MissingAuthor);

        act.Should().Throw<ArgumentException>().WithParameterName("bookId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Exclusion_key_rejects_a_blank_signal(string signalKey)
    {
        var act = () => new MetadataQualityExclusionKey(Guid.NewGuid(), signalKey);

        act.Should().Throw<ArgumentException>().WithParameterName("signalKey");
    }
}

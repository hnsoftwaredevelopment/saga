using EbookManager.Application.Metadata;
using FluentAssertions;

namespace EbookManager.Tests.Metadata;

public sealed class IsbnValidatorTests
{
    [Theory]
    [InlineData("0-306-40615-2", "0306406152")]
    [InlineData("0 8044 2957 X", "080442957X")]
    [InlineData("978-90-263-5660-5", "9789026356605")]
    public void TryNormalize_accepts_valid_isbn_10_and_isbn_13(string input, string expected)
    {
        IsbnValidator.TryNormalize(input, out var normalized).Should().BeTrue();
        normalized.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("niet-een-isbn")]
    [InlineData("9789026356601")]
    [InlineData("0306406153")]
    [InlineData("123456789")]
    public void TryNormalize_rejects_missing_malformed_and_wrong_check_digits(string? input)
    {
        IsbnValidator.TryNormalize(input, out var normalized).Should().BeFalse();
        normalized.Should().BeNull();
    }
}

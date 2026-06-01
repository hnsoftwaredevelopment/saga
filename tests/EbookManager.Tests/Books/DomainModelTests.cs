using EbookManager.Domain.Books;
using FluentAssertions;

namespace EbookManager.Tests.Books;

public sealed class DomainModelTests
{
    [Fact]
    public void Supported_formats_include_kobo_and_kindle_variants()
    {
        EbookFormatExtensions.Supported.Should().Contain([
            EbookFormat.Epub, EbookFormat.Kepub, EbookFormat.Pdf, EbookFormat.Cbr,
            EbookFormat.Cbz, EbookFormat.Mobi, EbookFormat.Azw, EbookFormat.Azw3, EbookFormat.Kfx
        ]);
    }

    [Theory]
    [InlineData("book.epub", EbookFormat.Epub)]
    [InlineData("book.kepub.epub", EbookFormat.Kepub)]
    [InlineData("BOOK.AZW3", EbookFormat.Azw3)]
    public void Filename_maps_to_expected_format(string filename, EbookFormat expected)
    {
        EbookFormatExtensions.TryFromFilename(filename, out var actual).Should().BeTrue();
        actual.Should().Be(expected);
    }
}

using EbookManager.Application.Metadata;

namespace EbookManager.Tests.TestSupport;

internal sealed class TestBookCoverImageValidator : IBookCoverImageValidator
{
    public bool TryValidateJpeg(byte[] bytes, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (bytes.Length < 11 || bytes[0] != 0xFF || bytes[1] != 0xD8)
        {
            return false;
        }

        height = (bytes[7] << 8) | bytes[8];
        width = (bytes[9] << 8) | bytes[10];
        return width > 0 && height > 0;
    }
}

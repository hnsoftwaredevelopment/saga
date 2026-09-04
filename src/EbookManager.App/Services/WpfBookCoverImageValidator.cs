using System.IO;
using System.Windows.Media.Imaging;
using EbookManager.Application.Metadata;

namespace EbookManager.App.Services;

public sealed class WpfBookCoverImageValidator : IBookCoverImageValidator
{
    public bool TryValidateJpeg(byte[] bytes, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (bytes.Length < 4 || bytes[0] != 0xFF || bytes[1] != 0xD8)
        {
            return false;
        }

        try
        {
            using var input = new MemoryStream(bytes, writable: false);
            var decoder = BitmapDecoder.Create(
                input,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
            if (decoder is not JpegBitmapDecoder || decoder.Frames.Count != 1)
            {
                return false;
            }

            width = decoder.Frames[0].PixelWidth;
            height = decoder.Frames[0].PixelHeight;
            return width > 0 && height > 0;
        }
        catch (Exception exception) when (exception is NotSupportedException or FileFormatException or IOException or ArgumentException)
        {
            width = 0;
            height = 0;
            return false;
        }
    }
}

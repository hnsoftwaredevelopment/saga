using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EbookManager.App.Services;

public static class AuthorPhotoImageProcessor
{
    public static async Task<byte[]> LoadAsync(string path)
    {
        return await Task.Run(async () =>
        {
            await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (file.Length is < 4 or > 10 * 1024 * 1024) throw new InvalidDataException("Photo size limit.");
            var bytes = new byte[(int)file.Length];
            await file.ReadExactlyAsync(bytes);
            using var input = new MemoryStream(bytes, writable: false);
            var decoder = BitmapDecoder.Create(input, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
            if (decoder is not JpegBitmapDecoder and not PngBitmapDecoder || decoder.Frames.Count != 1)
                throw new InvalidDataException("Expected JPEG or PNG.");
            var frame = decoder.Frames[0];
            if (frame.PixelWidth <= 0 || frame.PixelHeight <= 0 || (long)frame.PixelWidth * frame.PixelHeight > 25_000_000)
                throw new InvalidDataException("Photo dimensions limit.");
            input.Position = 0;
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = input;
            if (frame.PixelWidth >= frame.PixelHeight) image.DecodePixelWidth = Math.Min(1024, frame.PixelWidth);
            else image.DecodePixelHeight = Math.Min(1024, frame.PixelHeight);
            image.EndInit();
            image.Freeze();
            var encoder = new JpegBitmapEncoder { QualityLevel = 90 };
            encoder.Frames.Add(BitmapFrame.Create(image));
            using var output = new MemoryStream();
            encoder.Save(output);
            return output.ToArray();
        });
    }

    public static BitmapSource? Preview(byte[]? bytes)
    {
        if (bytes is null) return null;
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
            var frame = decoder.Frames[0];
            if ((long)frame.PixelWidth * frame.PixelHeight > 25_000_000) return null;
            stream.Position = 0;
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = 240;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception) { return null; }
    }
}

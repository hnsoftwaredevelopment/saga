namespace EbookManager.Domain.Books;

public enum ReadingStatus
{
    Unread,
    Reading,
    Read
}

public enum MetadataWriteBackStatus
{
    NotAttempted,
    Unsupported,
    Succeeded,
    Failed
}

public enum EbookFormat
{
    Epub,
    Kepub,
    Pdf,
    Cbr,
    Cbz,
    Mobi,
    Azw,
    Azw3,
    Kfx
}

public static class EbookFormatExtensions
{
    public static readonly IReadOnlySet<EbookFormat> Supported = Enum.GetValues<EbookFormat>().ToHashSet();

    public static bool TryFromFilename(string path, out EbookFormat format)
    {
        var name = Path.GetFileName(path).ToLowerInvariant();
        if (name.EndsWith(".kepub.epub", StringComparison.Ordinal))
        {
            format = EbookFormat.Kepub;
            return true;
        }

        return Enum.TryParse(Path.GetExtension(name).TrimStart('.'), true, out format);
    }

    public static string ToExtension(this EbookFormat format) => format switch
    {
        EbookFormat.Kepub => ".kepub.epub",
        _ => $".{format.ToString().ToLowerInvariant()}"
    };
}

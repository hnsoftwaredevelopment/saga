namespace EbookManager.Application.Metadata;

public interface IBookCoverImageValidator
{
    bool TryValidateJpeg(byte[] bytes, out int width, out int height);
}

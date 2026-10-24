namespace EbookManager.Application.Authors;

public interface IAuthorPhotoStore
{
    // Input has been decoded, bounded and re-encoded as JPEG by the image processor.
    Task<string> SaveJpegAsync(Guid authorId, byte[] jpeg, CancellationToken cancellationToken);
    Task<byte[]?> ReadAsync(Guid authorId, string relativePath, CancellationToken cancellationToken);
    Task DeleteAsync(Guid authorId, string relativePath, CancellationToken cancellationToken);
}

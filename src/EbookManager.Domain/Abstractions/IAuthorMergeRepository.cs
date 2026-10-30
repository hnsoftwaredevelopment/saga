using EbookManager.Domain.Authors;

namespace EbookManager.Domain.Abstractions;

public interface IAuthorMergeRepository
{
    Task<AuthorMergePreview?> PreviewMergeAsync(IReadOnlyCollection<Guid> authorIds, CancellationToken token);
    Task<AuthorMergeResult> MergeAsync(AuthorMergePreview preview, AuthorMergeSelection selection, string? targetPhotoPath, CancellationToken token);
}

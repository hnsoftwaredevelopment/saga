using EbookManager.Domain.Authors;

namespace EbookManager.Domain.Abstractions;

public interface IAuthorRenameRepository
{
    Task<AuthorRenamePreview?> PreviewRenameAsync(Guid authorId, CancellationToken cancellationToken);
    Task<AuthorRenameResult> RenameAsync(AuthorRenamePreview preview, string newName, CancellationToken cancellationToken);
}

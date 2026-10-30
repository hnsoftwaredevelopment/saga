using EbookManager.Domain.Tags;

namespace EbookManager.Domain.Abstractions;

public interface ITagRepository
{
    Task<TagPage> SearchAsync(string query, int skip, int take, CancellationToken token);
    Task<TagBookPage> ListBooksAsync(Guid tagId, int skip, int take, CancellationToken token);
    Task<TagRenamePreview> PreviewRenameAsync(Guid tagId, CancellationToken token);
    Task<TagRenameResult> RenameAsync(TagRenamePreview preview, string newName, CancellationToken token);
}

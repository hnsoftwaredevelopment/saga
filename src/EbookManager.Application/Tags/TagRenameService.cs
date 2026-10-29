using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Tags;

namespace EbookManager.Application.Tags;

public sealed record TagRenameCompletion(TagRenameResult Change, IReadOnlyList<TagSourceBook> Warnings);

public interface ITagRenameService
{
    Task<TagRenamePreview> PreviewAsync(Guid tagId, CancellationToken token);
    Task<TagRenameCompletion> RenameAsync(TagRenamePreview preview, string newName, CancellationToken token);
}

public sealed class TagRenameService(ITagRepository repository, BookService books) : ITagRenameService
{
    public Task<TagRenamePreview> PreviewAsync(Guid tagId, CancellationToken token) => repository.PreviewRenameAsync(tagId, token);

    public async Task<TagRenameCompletion> RenameAsync(TagRenamePreview preview, string newName, CancellationToken token)
    {
        var change = await repository.RenameAsync(preview, newName, token);
        if (change.Status != TagRenameStatus.Renamed) return new(change, []);
        var warnings = new List<TagSourceBook>();
        // Commit has completed: finish synchronizing current stored metadata, even after cancellation.
        foreach (var book in change.Books)
        {
            try
            {
                var result = await books.SynchronizeStoredMetadataAsync(book.Id, CancellationToken.None);
                if (result.Status != BookSaveStatus.Succeeded ||
                    result.FileResults.Any(file => file.Result.Status == MetadataWriteBackStatus.Failed)) warnings.Add(book);
            }
            catch (Exception) { warnings.Add(book); }
        }
        return new(change, warnings.AsReadOnly());
    }
}


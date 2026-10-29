using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Tags;

namespace EbookManager.Application.Tags;

public sealed record TagAssignmentCompletion(TagAssignmentResult Change, IReadOnlyList<TagSourceBook> Warnings);
public interface ITagAssignmentService
{
    Task<TagAssignmentPreview> PreviewAsync(Guid tagId, IReadOnlyCollection<Guid> bookIds, CancellationToken token);
    Task<TagAssignmentCompletion> ApplyAsync(TagAssignmentPreview preview, TagAssignmentRequest request, CancellationToken token);
}

public sealed class TagAssignmentService(ITagAssignmentRepository repository, BookService books) : ITagAssignmentService
{
    public Task<TagAssignmentPreview> PreviewAsync(Guid tagId, IReadOnlyCollection<Guid> bookIds, CancellationToken token) =>
        repository.PreviewAssignmentsAsync(tagId, bookIds, token);

    public async Task<TagAssignmentCompletion> ApplyAsync(TagAssignmentPreview preview, TagAssignmentRequest request, CancellationToken token)
    {
        var result = await repository.ApplyAssignmentsAsync(preview, request, token);
        if (result.Status != TagAssignmentStatus.Applied) return new(result, []);
        var warnings = new List<TagSourceBook>();
        // The database committed: finish existing metadata synchronization even if the caller cancels.
        foreach (var book in result.Books)
        {
            try
            {
                var saved = await books.SynchronizeStoredMetadataAsync(book.Id, CancellationToken.None);
                if (saved.Status != BookSaveStatus.Succeeded || saved.FileResults.Any(file => file.Result.Status == MetadataWriteBackStatus.Failed))
                    warnings.Add(book);
            }
            catch (Exception) { warnings.Add(book); }
        }
        return new(result, warnings.AsReadOnly());
    }
}

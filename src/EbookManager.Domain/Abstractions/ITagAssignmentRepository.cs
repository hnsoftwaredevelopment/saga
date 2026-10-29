using EbookManager.Domain.Tags;

namespace EbookManager.Domain.Abstractions;

public interface ITagAssignmentRepository
{
    Task<TagAssignmentPreview> PreviewAssignmentsAsync(Guid tagId, IReadOnlyCollection<Guid> bookIds, CancellationToken token);
    Task<TagAssignmentResult> ApplyAssignmentsAsync(TagAssignmentPreview preview, TagAssignmentRequest request, CancellationToken token);
}

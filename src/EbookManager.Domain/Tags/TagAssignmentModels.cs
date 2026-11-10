namespace EbookManager.Domain.Tags;

public enum TagAssignmentOperation { Replace, Remove, Add }
public sealed record TagAssignmentRequest(TagAssignmentOperation Operation, string? TargetName = null)
{
    public IReadOnlyList<string> TargetNames { get; init; } = [];
}
public sealed record TagAssignmentPreview(Guid TagId, string Name, IReadOnlyList<TagSourceBook> Books);
public enum TagAssignmentStatus { Applied, Unchanged, InvalidName, Conflict }
public sealed record TagAssignmentResult(TagAssignmentStatus Status, Guid SourceTagId, Guid? TargetTagId,
    string? TargetName, IReadOnlyList<TagSourceBook> Books);

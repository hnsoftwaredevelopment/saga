namespace EbookManager.Domain.Tags;

public enum TagAssignmentOperation { Replace, Remove }
public sealed record TagAssignmentRequest(TagAssignmentOperation Operation, string? TargetName = null);
public sealed record TagAssignmentPreview(Guid TagId, string Name, IReadOnlyList<TagSourceBook> Books);
public enum TagAssignmentStatus { Applied, Unchanged, InvalidName, Conflict }
public sealed record TagAssignmentResult(TagAssignmentStatus Status, Guid SourceTagId, Guid? TargetTagId,
    string? TargetName, IReadOnlyList<TagSourceBook> Books);

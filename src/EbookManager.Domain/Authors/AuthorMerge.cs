namespace EbookManager.Domain.Authors;

public sealed record AuthorMergePreview(IReadOnlyList<AuthorRenamePreview> Authors)
{
    public int BookCount => Authors.SelectMany(author => author.BookIds).Distinct().Count();
}
public sealed record AuthorMergeSelection(Guid TargetId, Guid? BiographyAuthorId, Guid? WebsiteAuthorId, Guid? PhotoAuthorId);
public enum AuthorMergeStatus { Merged, InvalidSelection, Conflict, DuplicateBook }
public sealed record AuthorMergeResult(AuthorMergeStatus Status, AuthorProfile? Profile, IReadOnlyList<AuthorBook> Books);

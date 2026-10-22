namespace EbookManager.Domain.Authors;

public sealed record AuthorRenamePreview(AuthorProfile Profile, IReadOnlyList<Guid> BookIds);
public enum AuthorRenameStatus { Renamed, Unchanged, InvalidName, NameExists, Conflict, DuplicateBook }
public sealed record AuthorRenameResult(AuthorRenameStatus Status, AuthorProfile? Profile, IReadOnlyList<Guid> BookIds);

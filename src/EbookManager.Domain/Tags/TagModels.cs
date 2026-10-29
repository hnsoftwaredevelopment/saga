namespace EbookManager.Domain.Tags;

public static class TagNames
{
    public static string Normalize(string name) => name.Trim().ToLowerInvariant();
    public static bool IsValid(string? name) => !string.IsNullOrWhiteSpace(name) && !name.Contains(';') && !name.Any(char.IsControl);
}

public sealed record TagSummary(Guid Id, string Name, int BookCount);
public sealed record TagPage(IReadOnlyList<TagSummary> Tags, int TotalCount);
public sealed record TagBook(Guid Id, string Title, string Authors);
public sealed record TagBookPage(IReadOnlyList<TagBook> Books, int TotalCount);
public sealed record TagSourceBook(Guid Id, string Title);
public sealed record TagRenamePreview(Guid TagId, string Name, IReadOnlyList<TagSourceBook> Books);
public enum TagRenameStatus { Renamed, Unchanged, InvalidName, NameExists, Conflict }
public sealed record TagRenameResult(TagRenameStatus Status, Guid TagId, string Name, IReadOnlyList<TagSourceBook> Books);

namespace EbookManager.Domain.Authors;

public sealed record AuthorProfile(Guid Id, string Name, string? Biography, string? WebsiteUrl, string? PhotoRelativePath);
public sealed record AuthorSummary(Guid Id, string Name, int BookCount);
public sealed record AuthorBook(Guid Id, string Title, string? Series);
public sealed record AuthorPage(IReadOnlyList<AuthorSummary> Authors, int TotalCount);
public enum AuthorProfileSaveStatus { Saved, Conflict }

namespace EbookManager.Domain.Settings;

public sealed record LibrarySortSettings(
    string? Bookshelf = null,
    string? Detailed = null,
    string? List = null);

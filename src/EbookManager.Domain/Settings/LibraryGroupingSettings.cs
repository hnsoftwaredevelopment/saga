namespace EbookManager.Domain.Settings;

public sealed record LibraryGroupingSettings(
    IReadOnlyList<string>? Bookshelf = null,
    IReadOnlyList<string>? Detailed = null,
    IReadOnlyList<string>? List = null);

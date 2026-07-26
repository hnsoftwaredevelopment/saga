namespace EbookManager.Domain.Settings;

public sealed record LibraryColumnSettings(
    IReadOnlyList<string>? Detailed = null,
    IReadOnlyList<string>? List = null);

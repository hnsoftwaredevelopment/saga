namespace EbookManager.Domain.Settings;

public sealed record LibraryViewLayoutSettings(
    IReadOnlyDictionary<string, LibraryViewLayoutSetting>? Views = null);

public sealed record LibraryViewLayoutSetting(
    IReadOnlyList<string>? Groupings = null,
    IReadOnlyList<string>? Columns = null,
    IReadOnlyDictionary<string, double>? ColumnWidths = null,
    string? Sort = null);

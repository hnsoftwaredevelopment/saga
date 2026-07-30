namespace EbookManager.Domain.Settings;

public sealed record LibraryColumnWidthSettings(
    IReadOnlyDictionary<string, double>? Detailed = null,
    IReadOnlyDictionary<string, double>? List = null,
    IReadOnlyDictionary<string, double>? DuplicateCandidates = null);

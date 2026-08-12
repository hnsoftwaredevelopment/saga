namespace EbookManager.Domain.Settings;

public sealed record LibraryViewDefinitionSettings(
    IReadOnlyList<LibraryViewDefinitionSetting>? CustomViews = null);

public sealed record LibraryViewDefinitionSetting(
    string Id,
    string Name,
    string BaseView,
    string LayoutKey);

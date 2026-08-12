namespace EbookManager.Presentation.ViewModels;

public sealed record LibraryViewDefinitionViewModel(
    string Id,
    string Name,
    LibraryView BaseView,
    string LayoutKey,
    bool IsBuiltIn);

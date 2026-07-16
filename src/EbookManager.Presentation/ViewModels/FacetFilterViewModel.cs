using CommunityToolkit.Mvvm.ComponentModel;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class FacetFilterViewModel(
    string name,
    int count,
    bool isSelected,
    Action selectionChanged,
    string? displayName = null)
    : ObservableObject
{
    private readonly Action selectionChanged = selectionChanged;

    public string Name { get; } = name;
    public int Count { get; } = count;
    public string DisplayName => $"{DisplayText} ({Count})";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    private string displayText = displayName ?? name;

    [ObservableProperty]
    private bool isSelected = isSelected;

    partial void OnIsSelectedChanged(bool value) => selectionChanged();
}

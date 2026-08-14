using CommunityToolkit.Mvvm.ComponentModel;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class LibraryColumnChoiceViewModel : ObservableObject
{
    public LibraryColumnChoiceViewModel(LibraryColumnKey key, string displayName, bool isSelected)
    {
        Key = key;
        this.displayName = displayName;
        this.isSelected = isSelected;
    }

    public LibraryColumnKey Key { get; }

    public LibraryColumnOption? Option => Key.StandardOption;

    [ObservableProperty]
    private string displayName;

    [ObservableProperty]
    private bool isSelected;
}

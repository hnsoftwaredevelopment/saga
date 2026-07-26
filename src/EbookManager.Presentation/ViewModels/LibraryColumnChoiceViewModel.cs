using CommunityToolkit.Mvvm.ComponentModel;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class LibraryColumnChoiceViewModel : ObservableObject
{
    public LibraryColumnChoiceViewModel(LibraryColumnOption option, bool isSelected)
    {
        Option = option;
        this.isSelected = isSelected;
    }

    public LibraryColumnOption Option { get; }

    [ObservableProperty]
    private bool isSelected;
}

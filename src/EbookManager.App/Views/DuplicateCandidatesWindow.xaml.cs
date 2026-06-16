using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class DuplicateCandidatesWindow : System.Windows.Window
{
    public DuplicateCandidatesWindow(DuplicateCandidatesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        Close();
    }
}

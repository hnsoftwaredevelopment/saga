using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class DuplicateMergePreviewWindow : System.Windows.Window
{
    public DuplicateMergePreviewWindow(DuplicateMergePreviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void LinkFormatClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        DialogResult = true;
    }
}

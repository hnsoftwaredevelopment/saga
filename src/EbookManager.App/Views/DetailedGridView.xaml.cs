namespace EbookManager.App.Views;

public partial class DetailedGridView : System.Windows.Controls.UserControl
{
    public DetailedGridView()
    {
        InitializeComponent();
    }

    private void BookRowMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is EbookManager.Presentation.ViewModels.LibraryViewModel viewModel &&
            sender is System.Windows.FrameworkElement { DataContext: EbookManager.Presentation.ViewModels.BookRowViewModel row })
        {
            viewModel.SelectedBook = row;
        }
    }
}

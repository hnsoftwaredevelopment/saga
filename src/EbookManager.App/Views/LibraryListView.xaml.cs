namespace EbookManager.App.Views;

public partial class LibraryListView : System.Windows.Controls.UserControl
{
    public LibraryListView()
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

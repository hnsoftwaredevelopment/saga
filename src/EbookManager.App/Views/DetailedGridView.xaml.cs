namespace EbookManager.App.Views;

public partial class DetailedGridView : System.Windows.Controls.UserControl
{
    private readonly LibraryGridColumnVisibility columnVisibility;

    public DetailedGridView()
    {
        InitializeComponent();
        columnVisibility = new LibraryGridColumnVisibility(BooksGrid, EbookManager.Presentation.ViewModels.LibraryView.Detailed);
        DataContextChanged += (_, e) => columnVisibility.Attach(e.NewValue as EbookManager.Presentation.ViewModels.LibraryViewModel);
        Loaded += (_, _) => columnVisibility.Attach(DataContext as EbookManager.Presentation.ViewModels.LibraryViewModel);
        Unloaded += (_, _) => columnVisibility.Detach();
    }

    private void BookRowMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is EbookManager.Presentation.ViewModels.LibraryViewModel viewModel &&
            sender is System.Windows.FrameworkElement { DataContext: EbookManager.Presentation.ViewModels.BookRowViewModel row })
        {
            viewModel.SelectedBook = row;
            viewModel.SetSelectedBooks([row]);
        }
    }

    private void BooksGridPreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is EbookManager.Presentation.ViewModels.LibraryViewModel viewModel)
        {
            LibraryGridSelectionHelper.SelectRowUnderPointer(BooksGrid, viewModel, e);
        }
    }

    private void BooksGridSelectionChanged(object sender, Syncfusion.UI.Xaml.Grid.GridSelectionChangedEventArgs e)
    {
        if (DataContext is EbookManager.Presentation.ViewModels.LibraryViewModel viewModel)
        {
            viewModel.SetSelectedBooks(BooksGrid.SelectedItems.OfType<EbookManager.Presentation.ViewModels.BookRowViewModel>());
        }
    }
}

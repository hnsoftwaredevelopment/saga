namespace EbookManager.App.Views;

public partial class DetailedGridView : System.Windows.Controls.UserControl
{
    private readonly LibraryGridColumnVisibility columnVisibility;

    public DetailedGridView()
    {
        InitializeComponent();
        LibraryGroupedHeaderResize.Attach(
            GroupedHeaderGrid,
            EbookManager.Presentation.ViewModels.LibraryView.Detailed,
            [
                EbookManager.Presentation.ViewModels.LibraryColumnOption.Cover,
                EbookManager.Presentation.ViewModels.LibraryColumnOption.Title,
                EbookManager.Presentation.ViewModels.LibraryColumnOption.Authors,
                EbookManager.Presentation.ViewModels.LibraryColumnOption.Format,
                EbookManager.Presentation.ViewModels.LibraryColumnOption.Series,
                EbookManager.Presentation.ViewModels.LibraryColumnOption.SeriesNumber,
                EbookManager.Presentation.ViewModels.LibraryColumnOption.Status,
                EbookManager.Presentation.ViewModels.LibraryColumnOption.Language,
                EbookManager.Presentation.ViewModels.LibraryColumnOption.Publisher,
                EbookManager.Presentation.ViewModels.LibraryColumnOption.PublicationDate,
                EbookManager.Presentation.ViewModels.LibraryColumnOption.Tags,
                EbookManager.Presentation.ViewModels.LibraryColumnOption.Isbn,
                EbookManager.Presentation.ViewModels.LibraryColumnOption.Description,
                EbookManager.Presentation.ViewModels.LibraryColumnOption.DateAdded,
                EbookManager.Presentation.ViewModels.LibraryColumnOption.LastModified,
                EbookManager.Presentation.ViewModels.LibraryColumnOption.EReader
            ]);
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
        }
    }
}

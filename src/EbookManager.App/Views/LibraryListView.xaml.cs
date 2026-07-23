namespace EbookManager.App.Views;

public partial class LibraryListView : System.Windows.Controls.UserControl
{
    private readonly LibraryGridGroupingProjectionBinder groupingProjectionBinder;

    public LibraryListView()
    {
        InitializeComponent();
        groupingProjectionBinder = new LibraryGridGroupingProjectionBinder(this, BooksGrid);
    }
}

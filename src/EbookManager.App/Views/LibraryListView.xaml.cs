namespace EbookManager.App.Views;

public partial class LibraryListView : System.Windows.Controls.UserControl
{
    private readonly LibraryGridGroupingBinder groupingBinder;

    public LibraryListView()
    {
        InitializeComponent();
        groupingBinder = new LibraryGridGroupingBinder(this, BooksGrid);
    }
}

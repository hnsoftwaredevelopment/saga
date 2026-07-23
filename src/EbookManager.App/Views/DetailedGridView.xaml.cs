namespace EbookManager.App.Views;

public partial class DetailedGridView : System.Windows.Controls.UserControl
{
    private readonly LibraryGridGroupingProjectionBinder groupingProjectionBinder;

    public DetailedGridView()
    {
        InitializeComponent();
        groupingProjectionBinder = new LibraryGridGroupingProjectionBinder(this, BooksGrid);
    }
}

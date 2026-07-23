namespace EbookManager.App.Views;

public partial class DetailedGridView : System.Windows.Controls.UserControl
{
    private readonly LibraryGridGroupingBinder groupingBinder;

    public DetailedGridView()
    {
        InitializeComponent();
        groupingBinder = new LibraryGridGroupingBinder(this, BooksGrid);
    }
}

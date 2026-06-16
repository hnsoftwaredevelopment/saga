using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class DuplicateCandidateDetailsWindow : System.Windows.Window
{
    public DuplicateCandidateDetailsWindow(DuplicateCandidateRowViewModel book)
    {
        InitializeComponent();
        DataContext = book;
    }

    private void CloseClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        Close();
    }
}

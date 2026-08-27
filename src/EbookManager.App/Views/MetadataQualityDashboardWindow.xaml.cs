using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class MetadataQualityDashboardWindow : System.Windows.Window
{
    public MetadataQualityDashboardWindow(MetadataQualityDashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseClicked(object sender, System.Windows.RoutedEventArgs e) => Close();

    private void OpenSelectedBookClicked(object sender, System.Windows.RoutedEventArgs e) =>
        ConfirmSelectedBook();

    private void BookRowMouseDoubleClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (FindAncestor<System.Windows.Controls.DataGridRow>(e.OriginalSource as System.Windows.DependencyObject) is not null)
        {
            ConfirmSelectedBook();
        }
    }

    private void ConfirmSelectedBook()
    {
        if (DataContext is MetadataQualityDashboardViewModel { CanOpenSelectedBook: true })
        {
            DialogResult = true;
        }
    }

    private static T? FindAncestor<T>(System.Windows.DependencyObject? source)
        where T : System.Windows.DependencyObject
    {
        while (source is not null)
        {
            if (source is T match)
            {
                return match;
            }

            source = System.Windows.Media.VisualTreeHelper.GetParent(source);
        }

        return null;
    }
}

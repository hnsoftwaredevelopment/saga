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

    private void BookRowMouseDoubleClicked(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
        ConfirmSelectedBook();

    private void ConfirmSelectedBook()
    {
        if (DataContext is MetadataQualityDashboardViewModel { CanOpenSelectedBook: true })
        {
            DialogResult = true;
        }
    }
}

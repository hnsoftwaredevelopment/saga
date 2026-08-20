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
}

using System.Windows;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class MetadataQualityTitleAuthorRepairWindow : Window
{
    public MetadataQualityTitleAuthorRepairWindow(MetadataQualityTitleAuthorRepairViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => ConfirmTitleAuthorRepairButton.Focus();
    }

    private void ConfirmClicked(object sender, RoutedEventArgs e) => DialogResult = true;
}

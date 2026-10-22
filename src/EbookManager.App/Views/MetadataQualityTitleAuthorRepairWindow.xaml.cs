using System.Windows;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class MetadataQualityTitleAuthorRepairWindow : Window
{
    public MetadataQualityTitleAuthorRepairWindow(MetadataQualityTitleAuthorRepairViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        if (viewModel.IsBulk)
        {
            Width = 760;
            Height = 600;
        }
        Loaded += (_, _) =>
        {
            if (viewModel.IsBulk)
            {
                SwapPreviews.SelectedIndex = 0;
                SwapPreviews.Focus();
            }
            else
            {
                ConfirmTitleAuthorRepairButton.Focus();
            }
        };
    }

    private void ConfirmClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MetadataQualityTitleAuthorRepairViewModel { CanConfirm: true })
        {
            DialogResult = true;
        }
    }
}

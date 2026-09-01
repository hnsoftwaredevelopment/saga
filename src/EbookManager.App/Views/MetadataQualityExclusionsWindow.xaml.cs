using EbookManager.Presentation.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace EbookManager.App.Views;

public partial class MetadataQualityExclusionsWindow : Window
{
    public MetadataQualityExclusionsWindow(MetadataQualityExclusionsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseClicked(object sender, RoutedEventArgs e) => Close();

    private void MetadataQualityExclusionsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MetadataQualityExclusionsViewModel viewModel)
        {
            viewModel.SetSelectedRows(
                MetadataQualityExclusionsGrid.SelectedItems.OfType<MetadataQualityExclusionRowViewModel>());
        }
    }

    private async void RestoreAllClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MetadataQualityExclusionsViewModel viewModel || !viewModel.HasRows)
        {
            return;
        }

        var confirmation = MessageBox.Show(
            this,
            Localization.LocalizedStrings.Current["RestoreAllQualityExclusionsConfirmationMessage"],
            Localization.LocalizedStrings.Current["RestoreAllQualityExclusionsConfirmationTitle"],
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirmation == MessageBoxResult.Yes)
        {
            await viewModel.RestoreAllCommand.ExecuteAsync(null);
        }
    }
}

using EbookManager.Presentation.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace EbookManager.App.Views;

public partial class DuplicateExclusionsWindow : Window
{
    public DuplicateExclusionsWindow(DuplicateExclusionsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void DuplicateExclusionsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is DuplicateExclusionsViewModel viewModel)
        {
            viewModel.SetSelectedRows(DuplicateExclusionsGrid.SelectedItems.OfType<DuplicateExclusionRowViewModel>());
        }
    }
}

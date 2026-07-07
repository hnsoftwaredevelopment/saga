using EbookManager.Presentation.ViewModels;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace EbookManager.App.Views;

public partial class DuplicateCandidatesWindow : System.Windows.Window
{
    public DuplicateCandidatesWindow(DuplicateCandidatesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        Close();
    }

    private void DuplicateRowDoubleClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRow { DataContext: DuplicateCandidateRowViewModel row })
        {
            e.Handled = true;
            ShowDetails(row);
        }
    }

    private void ShowDetailsClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is Button { DataContext: DuplicateCandidateRowViewModel row })
        {
            e.Handled = true;
            ShowDetails(row);
        }
    }

    private void ShowDetails(DuplicateCandidateRowViewModel row)
    {
        var window = new DuplicateCandidateDetailsWindow(row)
        {
            Owner = this
        };
        window.ShowDialog();
    }

    private async void DeleteCandidateButtonClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is Button { DataContext: DuplicateCandidateRowViewModel row })
        {
            e.Handled = true;
            await DeleteCandidateAsync(row);
        }
    }

    private async void DeleteCandidateClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DuplicateRowsGrid.SelectedItem is not DuplicateCandidateRowViewModel row)
        {
            return;
        }

        e.Handled = true;
        await DeleteCandidateAsync(row);
    }

    private async Task DeleteCandidateAsync(DuplicateCandidateRowViewModel row)
    {
        if (DataContext is not DuplicateCandidatesViewModel viewModel)
        {
            return;
        }

        await viewModel.DeleteCandidateAsync(row, CancellationToken.None);
        if (!viewModel.HasGroups)
        {
            Close();
        }
    }

    private void DuplicateRowsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is DuplicateCandidatesViewModel viewModel)
        {
            viewModel.SetSelectedRows(DuplicateRowsGrid.SelectedItems.OfType<DuplicateCandidateRowViewModel>());
        }
    }
}

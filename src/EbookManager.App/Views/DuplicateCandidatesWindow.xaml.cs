using EbookManager.Presentation.ViewModels;
using EbookManager.App.Localization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace EbookManager.App.Views;

public partial class DuplicateCandidatesWindow : System.Windows.Window
{
    private bool isMergingCandidate;

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
        if (IsInsideButton(e.OriginalSource as System.Windows.DependencyObject))
        {
            e.Handled = true;
            return;
        }

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

    private async void MergeCandidateButtonClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is Button { DataContext: DuplicateCandidateRowViewModel row } button)
        {
            e.Handled = true;
            if (isMergingCandidate)
            {
                return;
            }

            isMergingCandidate = true;
            button.IsEnabled = false;
            try
            {
                await MergeCandidateAsync(row);
            }
            finally
            {
                isMergingCandidate = false;
                button.IsEnabled = true;
            }
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

    private async Task MergeCandidateAsync(DuplicateCandidateRowViewModel row)
    {
        if (DataContext is not DuplicateCandidatesViewModel viewModel)
        {
            return;
        }

        var preview = viewModel.CreateMergePreview(row);
        if (preview is null)
        {
            return;
        }

        var previewWindow = new DuplicateMergePreviewWindow(preview)
        {
            Owner = this
        };
        if (previewWindow.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await viewModel.MergeCandidateAsync(preview, CancellationToken.None);
            if (!viewModel.HasGroups)
            {
                Close();
            }
        }
        catch (Exception)
        {
            System.Windows.MessageBox.Show(
                this,
                LocalizedStrings.Current["DuplicateMergeFailedMessage"],
                LocalizedStrings.Current["DuplicateMergeFailedTitle"],
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            Close();
        }
    }

    private static bool IsInsideButton(System.Windows.DependencyObject? source)
    {
        for (var current = source; current is not null; current = GetParent(current))
        {
            if (current is ButtonBase)
            {
                return true;
            }

            if (current is DataGridRow)
            {
                return false;
            }
        }

        return false;
    }

    private static DependencyObject? GetParent(DependencyObject current) =>
        current is Visual or Visual3D
            ? VisualTreeHelper.GetParent(current)
            : LogicalTreeHelper.GetParent(current);

    private void DuplicateRowsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is DuplicateCandidatesViewModel viewModel)
        {
            viewModel.SetSelectedRows(DuplicateRowsGrid.SelectedItems.OfType<DuplicateCandidateRowViewModel>());
        }
    }
}

using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using EbookManager.Presentation.ViewModels;
using Syncfusion.UI.Xaml.Grid;

namespace EbookManager.App.Views;

internal sealed class LibraryGridSelectionSynchronization(SfDataGrid grid, LibraryView view)
{
    private LibraryViewModel? viewModel;
    private bool applying;
    private bool queued;

    public void Attach(LibraryViewModel? value)
    {
        if (viewModel is not null) viewModel.PropertyChanged -= Changed;
        viewModel = value;
        if (value is not null) value.PropertyChanged += Changed;
        QueueApply();
    }

    public void RecordUserSelection()
    {
        if (applying || viewModel is not { } vm || vm.IsReconcilingBookSelection ||
            vm.SelectedView != view || !grid.IsVisible || !grid.IsKeyboardFocusWithin) return;
        vm.SetSelectedBooks(grid.SelectedItems.OfType<BookRowViewModel>());
        if (grid.SelectedItem is BookRowViewModel row) vm.SelectedBook = row;
    }

    private void Changed(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(LibraryViewModel.SelectedBookCount) or nameof(LibraryViewModel.SelectedView)
            or nameof(LibraryViewModel.IsReconcilingBookSelection) or nameof(LibraryViewModel.DetailedVisibleBooksSource)
            or nameof(LibraryViewModel.ListVisibleBooksSource)) QueueApply();
    }

    private void QueueApply()
    {
        if (queued) return;
        queued = true;
        grid.Dispatcher.BeginInvoke(() =>
        {
            queued = false;
            if (viewModel is not { } vm || vm.IsReconcilingBookSelection || vm.SelectedView != view || !grid.IsVisible) return;
            var selected = vm.SelectedBooks.ToArray();
            if (grid.SelectedItems.OfType<BookRowViewModel>().SequenceEqual(selected)) return;
            applying = true;
            try
            {
                grid.SelectedItems.Clear();
                foreach (var row in selected) grid.SelectedItems.Add(row);
            }
            finally { applying = false; }
        }, DispatcherPriority.Loaded);
    }
}

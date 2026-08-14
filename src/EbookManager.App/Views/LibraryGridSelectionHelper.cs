using EbookManager.Presentation.ViewModels;
using Syncfusion.UI.Xaml.Grid;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace EbookManager.App.Views;

internal static class LibraryGridSelectionHelper
{
    public static void SelectRowUnderPointer(
        SfDataGrid grid,
        LibraryViewModel viewModel,
        System.Windows.Input.MouseButtonEventArgs e)
    {
        if (FindBookRow(e.OriginalSource as DependencyObject) is not { } row)
        {
            return;
        }

        viewModel.SelectedBook = row;
        if (grid.SelectedItems.Contains(row))
        {
            viewModel.SetSelectedBooks(grid.SelectedItems.OfType<BookRowViewModel>());
            return;
        }

        grid.SelectedItems.Clear();
        grid.SelectedItems.Add(row);
        viewModel.SetSelectedBooks([row]);
    }

    private static BookRowViewModel? FindBookRow(DependencyObject? source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is FrameworkElement { DataContext: BookRowViewModel row })
            {
                return row;
            }

            current = current is Visual or Visual3D
                ? VisualTreeHelper.GetParent(current)
                : null;
        }

        return null;
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using EbookManager.Presentation.ViewModels;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.ScrollAxis;

namespace EbookManager.App.Views;

internal static class BookRevealScrollHelper
{
    public static void ScrollGridToBook(SfDataGrid grid, LibraryViewModel viewModel, Guid bookId)
    {
        var row = viewModel.VisibleBooks.FirstOrDefault(candidate => candidate.Id == bookId);
        if (row is null)
        {
            return;
        }

        var rowIndex = grid.ResolveToRowIndex(row);
        if (rowIndex >= 0)
        {
            grid.ScrollInView(new RowColumnIndex(rowIndex, 0));
        }
    }

    public static void ScrollListToBook(ListBox list, LibraryViewModel viewModel, Guid bookId, bool grouped)
    {
        if (!grouped)
        {
            var row = viewModel.VisibleBooks.FirstOrDefault(candidate => candidate.Id == bookId);
            if (row is not null)
            {
                list.ScrollIntoView(row);
            }

            return;
        }

        var topLevelGroup = viewModel.GroupedLibraryNodes.FirstOrDefault(group => group.ContainsBook(bookId));
        if (topLevelGroup is null)
        {
            return;
        }

        list.ScrollIntoView(topLevelGroup);
        list.UpdateLayout();
        list.Dispatcher.BeginInvoke(
            () => FindBookElement(list, bookId)?.BringIntoView(),
            DispatcherPriority.Loaded);
    }

    private static FrameworkElement? FindBookElement(DependencyObject root, Guid bookId)
    {
        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is FrameworkElement { DataContext: BookRowViewModel row } element && row.Id == bookId)
            {
                return element;
            }

            var match = FindBookElement(child, bookId);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }
}

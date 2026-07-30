using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

internal static class LibraryGroupedHeaderResize
{
    private const double GripWidth = 8;
    private const double MinimumColumnWidth = 40;

    public static void Attach(
        Grid headerGrid,
        LibraryView view,
        IReadOnlyList<LibraryColumnOption> columns)
    {
        for (var index = 0; index < columns.Count - 1 && index < headerGrid.ColumnDefinitions.Count; index++)
        {
            var option = columns[index];
            var thumb = new Thumb
            {
                Width = GripWidth,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Stretch,
                Cursor = Cursors.SizeWE,
                Background = System.Windows.Media.Brushes.Transparent,
                Tag = new ResizeState(view, option)
            };

            Grid.SetColumn(thumb, index);
            Panel.SetZIndex(thumb, 10);
            thumb.DragDelta += HeaderResizeDragDelta;
            thumb.DragCompleted += HeaderResizeDragCompleted;
            headerGrid.Children.Add(thumb);
        }
    }

    private static void HeaderResizeDragDelta(object sender, DragDeltaEventArgs e)
    {
        var columnIndex = sender is Thumb sourceThumb ? Grid.GetColumn(sourceThumb) : -1;
        if (sender is not Thumb thumb ||
            thumb.Parent is not Grid grid ||
            columnIndex < 0 ||
            columnIndex >= grid.ColumnDefinitions.Count)
        {
            return;
        }

        var column = grid.ColumnDefinitions[columnIndex];
        var currentWidth = column.ActualWidth > 0 ? column.ActualWidth : column.Width.Value;
        var nextWidth = Math.Max(MinimumColumnWidth, currentWidth + e.HorizontalChange);
        column.Width = new GridLength(nextWidth);
    }

    private static async void HeaderResizeDragCompleted(object sender, DragCompletedEventArgs e)
    {
        var columnIndex = sender is Thumb sourceThumb ? Grid.GetColumn(sourceThumb) : -1;
        if (sender is not Thumb { Tag: ResizeState state } thumb ||
            thumb.Parent is not Grid grid ||
            grid.DataContext is not LibraryViewModel viewModel ||
            columnIndex < 0 ||
            columnIndex >= grid.ColumnDefinitions.Count)
        {
            return;
        }

        var width = grid.ColumnDefinitions[columnIndex].ActualWidth;
        try
        {
            await viewModel.SetColumnWidthAsync(state.View, state.Column, width, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
        }
    }

    private sealed record ResizeState(LibraryView View, LibraryColumnOption Column);
}

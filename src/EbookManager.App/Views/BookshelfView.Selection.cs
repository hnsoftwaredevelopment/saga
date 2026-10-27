using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class BookshelfView
{
    private int tileFocusVersion;

    private static ListBoxItem? BookContainer(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is ListBoxItem { DataContext: BookRowViewModel } item) return item;
            source = source is Visual ? VisualTreeHelper.GetParent(source) : null;
        }
        return null;
    }

    private void BookPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not LibraryViewModel vm || BookContainer(e.OriginalSource as DependencyObject) is not { DataContext: BookRowViewModel row } item) return;
        // Nested lists must not clear another group's selection through their native selection logic.
        e.Handled = true;
        ++tileFocusVersion;
        item.Focus();
        vm.SelectBookshelfBook(row, Keyboard.Modifiers.HasFlag(ModifierKeys.Control), Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
    }

    private void BookPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not LibraryViewModel vm || BookContainer(e.OriginalSource as DependencyObject) is not { DataContext: BookRowViewModel row } item) return;
        var control = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        if (e.Key == Key.Escape) { vm.SetSelectedBooks([]); e.Handled = true; return; }
        if (e.Key == Key.Space) { vm.SelectBookshelfBook(row, control, shift); e.Handled = true; return; }
        if (e.Key is not (Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End)) return;
        e.Handled = true;
        var order = vm.GetBookshelfSelectionOrder();
        var index = order.ToList().FindIndex(book => book.Id == row.Id);
        if (index < 0 || order.Count == 0) return;
        var list = ItemsControl.ItemsControlFromItemContainer(item) as ListBox;
        var columns = Math.Max(1, (int)Math.Floor((list?.ActualWidth ?? 170) / 170));
        var next = e.Key switch
        {
            Key.Home => 0, Key.End => order.Count - 1,
            Key.Left => index - 1, Key.Right => index + 1,
            Key.Up => index - columns, _ => index + columns
        };
        var target = order[Math.Clamp(next, 0, order.Count - 1)];
        if (!control || shift) vm.SelectBookshelfBook(target, control, shift);
        FocusBook(vm, target.Id);
    }

    private void FocusBook(LibraryViewModel vm, Guid id)
    {
        var version = ++tileFocusVersion;
        if (!vm.IsBookshelfGrouped)
        {
            var index = vm.VisibleBooks.ToList().FindIndex(row => row.Id == id);
            BookshelfBooksList.ScrollIntoView(vm.VisibleBooks[index]);
            BookshelfBooksList.UpdateLayout();
        }
        else
        {
            var group = vm.GroupedLibraryNodes.FirstOrDefault(group => ContainsExpandedBook(group, id));
            if (group is null) return;
            BookshelfGroupsList.ScrollIntoView(group);
            BookshelfGroupsList.UpdateLayout();
            FindBookList(BookshelfGroupsList, id)?.ScrollIntoView(vm.VisibleBooks.First(row => row.Id == id));
            BookshelfGroupsList.UpdateLayout();
        }
        Dispatcher.BeginInvoke(() =>
        {
            if (version != tileFocusVersion || !IsVisible || !ReferenceEquals(DataContext, vm)) return;
            var container = FindBookContainer(vm.IsBookshelfGrouped ? BookshelfGroupsList : BookshelfBooksList, id);
            container?.BringIntoView();
            container?.Focus();
        }, DispatcherPriority.ContextIdle);
    }

    private static bool ContainsExpandedBook(LibraryGroupNodeViewModel group, Guid id) => group.IsExpanded &&
        (group.Books.Any(row => row.Id == id) || group.Groups.Any(child => ContainsExpandedBook(child, id)));

    private static ListBox? FindBookList(DependencyObject root, Guid id)
    {
        if (root is ListBox list && list.Items.OfType<BookRowViewModel>().Any(row => row.Id == id)) return list;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            if (FindBookList(VisualTreeHelper.GetChild(root, index), id) is { } found) return found;
        return null;
    }

    private static ListBoxItem? FindBookContainer(DependencyObject root, Guid id)
    {
        if (root is ListBoxItem { DataContext: BookRowViewModel row } item && row.Id == id) return item;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            if (FindBookContainer(VisualTreeHelper.GetChild(root, index), id) is { } found) return found;
        return null;
    }
}

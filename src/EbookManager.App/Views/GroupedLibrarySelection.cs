using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

/// <summary>Routes grouped rows through the same unique-book selection policy as tiles.</summary>
internal sealed class GroupedLibrarySelection
{
    private readonly UserControl view;
    private readonly ListBox groups;
    private int focusVersion;

    public GroupedLibrarySelection(UserControl view, ListBox groups)
    {
        this.view = view;
        this.groups = groups;
        view.PreviewMouseLeftButtonDown += MouseDown;
        view.PreviewKeyDown += KeyDown;
    }

    private void MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (view.DataContext is not LibraryViewModel { IsLibraryGrouped: true } vm ||
            BookContainer(e.OriginalSource as DependencyObject) is not { DataContext: BookRowViewModel row } item) return;
        // Handle before the nested ListBox can discard selection in other groups.
        e.Handled = true;
        ++focusVersion;
        item.Focus();
        vm.SelectBookshelfBook(row, Keyboard.Modifiers.HasFlag(ModifierKeys.Control), Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
    }

    private void KeyDown(object sender, KeyEventArgs e)
    {
        if (view.DataContext is not LibraryViewModel { IsLibraryGrouped: true } vm ||
            BookContainer(e.OriginalSource as DependencyObject) is not { DataContext: BookRowViewModel row }) return;
        var control = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        if (e.Key == Key.Escape) { ++focusVersion; vm.SetSelectedBooks([]); e.Handled = true; return; }
        if (e.Key == Key.Space) { ++focusVersion; vm.SelectBookshelfBook(row, control, shift); e.Handled = true; return; }
        if (e.Key is not (Key.Up or Key.Down or Key.Home or Key.End)) return;
        e.Handled = true;
        var order = vm.GetBookshelfSelectionOrder();
        var index = order.ToList().FindIndex(book => book.Id == row.Id);
        if (index < 0) return;
        var next = e.Key switch
        {
            Key.Home => 0, Key.End => order.Count - 1,
            Key.Up => index - 1, _ => index + 1
        };
        var target = order[Math.Clamp(next, 0, order.Count - 1)];
        if (!control || shift) vm.SelectBookshelfBook(target, control, shift);
        FocusBook(vm, target.Id);
    }

    private void FocusBook(LibraryViewModel vm, Guid id)
    {
        var version = ++focusVersion;
        var selectedView = vm.SelectedView;
        var group = vm.GroupedLibraryNodes.FirstOrDefault(group => ContainsExpandedBook(group, id));
        if (group is null) return;
        groups.ScrollIntoView(group);
        groups.UpdateLayout();
        var list = Descendants<ListBox>(groups).FirstOrDefault(list => list.Items.OfType<BookRowViewModel>().Any(row => row.Id == id));
        list?.ScrollIntoView(list.Items.OfType<BookRowViewModel>().First(row => row.Id == id));
        groups.UpdateLayout();
        var focusAtRequest = Keyboard.FocusedElement;
        view.Dispatcher.BeginInvoke(() =>
        {
            if (version != focusVersion || !view.IsVisible || !ReferenceEquals(view.DataContext, vm) ||
                selectedView != vm.SelectedView || !vm.GetBookshelfSelectionOrder().Any(row => row.Id == id) ||
                !ReferenceEquals(Keyboard.FocusedElement, focusAtRequest)) return;
            var item = Descendants<ListBoxItem>(groups).FirstOrDefault(item => item.DataContext is BookRowViewModel row && row.Id == id);
            item?.BringIntoView();
            item?.Focus();
        }, DispatcherPriority.ContextIdle);
    }

    private static bool ContainsExpandedBook(LibraryGroupNodeViewModel group, Guid id) => group.IsExpanded &&
        (group.Books.Any(row => row.Id == id) || group.Groups.Any(child => ContainsExpandedBook(child, id)));

    private static ListBoxItem? BookContainer(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is ListBoxItem { DataContext: BookRowViewModel } item) return item;
            source = source is Visual ? VisualTreeHelper.GetParent(source)
                : source is FrameworkContentElement content ? content.Parent : LogicalTreeHelper.GetParent(source);
        }
        return null;
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            foreach (var child in Descendants<T>(VisualTreeHelper.GetChild(root, index))) yield return child;
    }
}

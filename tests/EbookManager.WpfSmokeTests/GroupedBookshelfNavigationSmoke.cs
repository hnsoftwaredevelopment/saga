using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using EbookManager.App.Views;
using EbookManager.Application.Books;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;
using EbookManager.Infrastructure.Files;
using EbookManager.Infrastructure.Metadata;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.WpfSmokeTests;

internal static class GroupedBookshelfNavigationSmoke
{
    public static async Task RunAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var books = Enumerable.Range(0, 12).Select(index => new Book(Guid.NewGuid(),
            new BookMetadata($"Book {index:D2}", ["Author"], Tags: [index < 2 ? "A" : index < 7 ? "B" : index < 8 ? "C" : "D"]),
            ReadingStatus.Unread, null, now, now)).ToArray();
        var repository = new SmokeBookRepository(books);
        var service = new BookService(repository, new ManagedLibraryFileStore(System.IO.Path.GetTempPath()), new MetadataAdapterResolver([new FallbackMetadataAdapter()]));
        var vm = new LibraryViewModel(repository, new BookSearchService(), new BookDetailsViewModel(service), new SmokeInteraction(),
            bookService: service, searchFilterDelay: _ => Task.CompletedTask);
        await vm.RefreshAsync();
        vm.SelectedView = LibraryView.Bookshelf;
        vm.SetGroupingOptions([LibraryGroupOption.Tag]);
        foreach (var group in vm.GroupedLibraryNodes) group.IsExpanded = true;
        var view = new BookshelfView { DataContext = vm };
        var window = new Window { Content = view, Width = 700, Height = 1000 };
        try
        {
            window.Show(); window.Activate(); await IdleAsync();
            async Task Navigate(int from, Key key, int expected, ModifierKeys modifiers = ModifierKeys.None)
            {
                vm.SelectBookshelfBook(vm.VisibleBooks[from], false, false);
                var groups = (ListBox)view.FindName("BookshelfGroupsList");
                groups.ScrollIntoView(vm.GroupedLibraryNodes.First(group => group.ContainsBook(books[from].Id)));
                groups.UpdateLayout();
                FindBookList(groups, vm.VisibleBooks[from])?.ScrollIntoView(vm.VisibleBooks[from]);
                groups.UpdateLayout();
                await IdleAsync();
                var item = Containers(view).FirstOrDefault(item => ((BookRowViewModel)item.DataContext).Id == books[from].Id)
                    ?? throw new InvalidOperationException($"Source tile {from} was not realized.");
                item.BringIntoView(); item.Focus();
                GroupedBookSelectionSmoke.WithModifiers(modifiers, () => item.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,
                    PresentationSource.FromVisual(item), Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent }));
                await IdleAsync();
                Require(vm.SelectedBook?.Id == books[expected].Id,
                    $"Grouped {key} from {from} selected {vm.SelectedBook?.Title}, expected book {expected}.");
                Require((Keyboard.FocusedElement as FrameworkElement)?.DataContext is BookRowViewModel focus && focus.Id == books[expected].Id,
                    "Grouped navigation did not focus its selected target.");
            }
            // At this width group A has a partial row, B has two rows, and C only one tile.
            await Navigate(0, Key.Down, 2);
            await Navigate(1, Key.Down, 3);
            await Navigate(5, Key.Up, 2);
            await Navigate(4, Key.Down, 6);
            await Navigate(6, Key.Down, 7);
            await Navigate(7, Key.Up, 5);
            await Navigate(7, Key.Down, 8);
            await Navigate(8, Key.Up, 7);
            await Navigate(0, Key.Up, 0);
            await Navigate(11, Key.Down, 11);
            await Navigate(0, Key.Down, 2, ModifierKeys.Shift);
            Require(vm.SelectedBooks.Select(row => row.Id).ToHashSet().SetEquals(books.Take(3).Select(book => book.Id)),
                "Shift+Down included a skipped target from the next group.");
            vm.GroupedLibraryNodes[1].IsExpanded = false;
            await IdleAsync();
            await Navigate(0, Key.Down, 7);
            Console.WriteLine("PASS: grouped bookshelf Up/Down respects partial rows, group columns, collapsed groups and Shift ranges.");
        }
        finally { window.Close(); }
    }
    public static async Task RunDuplicateAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var books = Enumerable.Range(0, 5).Select(index => new Book(Guid.NewGuid(),
            new BookMetadata($"Book {index:D2}", ["Author"], Tags: index == 0 ? ["A", "B"] : [index == 1 ? "A" : "B"]),
            ReadingStatus.Unread, null, now, now)).ToArray();
        var repository = new SmokeBookRepository(books);
        var service = new BookService(repository, new ManagedLibraryFileStore(System.IO.Path.GetTempPath()), new MetadataAdapterResolver([new FallbackMetadataAdapter()]));
        var vm = new LibraryViewModel(repository, new BookSearchService(), new BookDetailsViewModel(service), new SmokeInteraction(),
            bookService: service, searchFilterDelay: _ => Task.CompletedTask);
        await vm.RefreshAsync();
        vm.SelectedView = LibraryView.Bookshelf;
        vm.SetGroupingOptions([LibraryGroupOption.Tag]);
        foreach (var group in vm.GroupedLibraryNodes) group.IsExpanded = true;
        var view = new BookshelfView { DataContext = vm };
        var window = new Window { Content = view, Width = 700, Height = 850 };
        try
        {
            window.Show(); window.Activate(); await IdleAsync();
            async Task Press(int sourceGroup, int sourceBook, Key key, int expectedGroup, int expectedBook, ModifierKeys modifiers = ModifierKeys.None)
            {
                var groups = (ListBox)view.FindName("BookshelfGroupsList");
                groups.ScrollIntoView(vm.GroupedLibraryNodes[sourceGroup]); groups.UpdateLayout();
                var list = GroupList(groups, vm.GroupedLibraryNodes[sourceGroup]) ?? throw new InvalidOperationException("Source group not realized.");
                list.ScrollIntoView(vm.VisibleBooks[sourceBook]); list.UpdateLayout();
                var item = (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(vm.VisibleBooks[sourceBook]);
                vm.SelectBookshelfBook(vm.VisibleBooks[sourceBook], false, false);
                item.Focus();
                GroupedBookSelectionSmoke.WithModifiers(modifiers, () => item.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,
                    PresentationSource.FromVisual(item), Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent }));
                await IdleAsync();
                Require(Keyboard.FocusedElement is ListBoxItem focused && focused.DataContext is BookRowViewModel row && row.Id == books[expectedBook].Id &&
                    ReferenceEquals(ItemsControl.ItemsControlFromItemContainer(focused).ItemsSource, vm.GroupedLibraryNodes[expectedGroup].Books),
                    $"{key} focused the wrong occurrence of book {expectedBook}; expected group {expectedGroup}.");
                Require(vm.SelectedBooks.Select(row => row.Id).Distinct().Count() == vm.SelectedBookCount, "Duplicate occurrences were counted twice.");
            }
            await Press(0, 0, Key.Down, 1, 0);
            await Press(1, 0, Key.Down, 1, 4);
            await Press(1, 0, Key.Up, 0, 0);
            await Press(0, 0, Key.Down, 1, 0, ModifierKeys.Shift);
            Require(vm.SelectedBookCount == 1 && vm.SelectedBooks.Single().Id == books[0].Id, "Shift selected a duplicate book twice.");
            await Press(0, 0, Key.Down, 1, 0, ModifierKeys.Control);
            Require(vm.SelectedBooks.Single().Id == books[0].Id, "Ctrl navigation changed the selected IDs.");
            Console.WriteLine("PASS: duplicate tagged tiles keep vertical focus in the intended group, including Shift and Ctrl.");
        }
        finally { window.Close(); }
    }
    private static ListBox? GroupList(DependencyObject root, LibraryGroupNodeViewModel group)
    {
        if (root is ListBox list && ReferenceEquals(list.ItemsSource, group.Books)) return list;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            if (GroupList(VisualTreeHelper.GetChild(root, index), group) is { } found) return found;
        return null;
    }
    private static Task IdleAsync() => Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static ListBox? FindBookList(DependencyObject root, BookRowViewModel row)
    {
        if (root is ListBox list && list.Items.Contains(row)) return list;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            if (FindBookList(VisualTreeHelper.GetChild(root, index), row) is { } found) return found;
        return null;
    }
    private static IEnumerable<ListBoxItem> Containers(DependencyObject root)
    {
        if (root is ListBoxItem { DataContext: BookRowViewModel } item) yield return item;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            foreach (var child in Containers(VisualTreeHelper.GetChild(root, index))) yield return child;
    }
}

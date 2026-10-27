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

internal static class BookshelfSelectionSmoke
{
    public static async Task RunAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var books = Enumerable.Range(0, 80).Select(index => new Book(Guid.NewGuid(),
            new BookMetadata($"Book {index:D3}", ["Author"], Tags: [index < 40 ? "First" : "Second"]), ReadingStatus.Unread, null, now, now)).ToArray();
        var repository = new SmokeBookRepository(books);
        var service = new BookService(repository, new ManagedLibraryFileStore(System.IO.Path.GetTempPath()), new MetadataAdapterResolver([new FallbackMetadataAdapter()]));
        var vm = new LibraryViewModel(repository, new BookSearchService(), new BookDetailsViewModel(service), new SmokeInteraction(),
            bookService: service, searchFilterDelay: _ => Task.CompletedTask);
        await vm.RefreshAsync();
        vm.SelectedView = LibraryView.Bookshelf;
        var view = new BookshelfView { DataContext = vm };
        var window = new Window { Content = view, Width = 800, Height = 600 };
        try
        {
            window.Show(); window.Activate();
            await IdleAsync();
            var first = Containers(view).First(item => item.DataContext is BookRowViewModel row && row.Id == books[0].Id);
            first.Focus();
            SendKey(first, Key.Right);
            await IdleAsync();
            Require(vm.SelectedBook?.Id == books[1].Id && vm.SelectedBookCount == 1, "Arrow selection did not select the next tile.");
            var focused = Keyboard.FocusedElement as ListBoxItem;
            Require(focused?.DataContext is BookRowViewModel focusedRow && focusedRow.Id == books[1].Id, "Arrow selection lost tile focus.");
            SendKey(focused!, Key.End);
            await IdleAsync();
            Require(vm.SelectedBook?.Id == books[^1].Id && (Keyboard.FocusedElement as FrameworkElement)?.DataContext is BookRowViewModel last && last.Id == books[^1].Id,
                "End did not realize, scroll to and focus the last virtualized tile.");
            Require(Containers(view).Count() < 40, "Book selection realized the entire tile library.");
            Console.WriteLine("PASS: bookshelf keyboard navigation focuses virtualized tiles without realizing the full library.");
            vm.SelectBookshelfBook(vm.VisibleBooks[1], true, false);
            vm.SelectBookshelfBook(vm.VisibleBooks[5], false, true);
            await IdleAsync();
            Require(vm.SelectedBookCount == 5 && vm.SelectedBooks.All(row => row.IsSelected), "Tile range did not match the central bulk selection.");
            vm.SearchText = "Book 003";
            await IdleAsync();
            Require(vm.SelectedBookCount == 1 && vm.SelectedBooks.Single().Id == books[3].Id, "Filtering retained hidden selected books.");
            vm.SearchText = "";
            await IdleAsync();
            Require(vm.SelectedBookCount == 1, "Clearing the search resurrected hidden selection.");
            Console.WriteLine("PASS: bookshelf ranges and filtering use the same unique central selection.");
            vm.SetGroupingOptions([LibraryGroupOption.Tag]);
            foreach (var group in vm.GroupedLibraryNodes) group.IsExpanded = true;
            await IdleAsync();
            vm.SelectBookshelfBook(vm.VisibleBooks[0], false, false);
            vm.SelectBookshelfBook(vm.VisibleBooks[41], true, false);
            Require(vm.SelectedBookCount == 2, "Cross-group selection lost books from another group.");
            var groupedFirst = Containers(view).First(item => item.DataContext is BookRowViewModel row && row.Id == books[0].Id);
            groupedFirst.Focus();
            SendKey(groupedFirst, Key.End);
            await IdleAsync();
            Require((Keyboard.FocusedElement as FrameworkElement)?.DataContext is BookRowViewModel groupedLast && groupedLast.Id == books[^1].Id,
                "Grouped keyboard navigation did not realize and focus an offscreen book.");
            Console.WriteLine("PASS: grouped bookshelf selection spans open groups.");
            vm.SetGroupingOptions([]);
            vm.SetSelectedBooks([vm.VisibleBooks[0], vm.VisibleBooks[2]]);
            var expected = vm.SelectedBooks.Select(row => row.Id).ToHashSet();
            var listView = new LibraryListView { DataContext = vm, Visibility = Visibility.Collapsed };
            var detailedView = new DetailedGridView { DataContext = vm, Visibility = Visibility.Collapsed };
            window.Content = null;
            var host = new Grid();
            host.Children.Add(view); host.Children.Add(listView); host.Children.Add(detailedView);
            window.Content = host;
            foreach (var (mode, control) in new[] { (LibraryView.List, (FrameworkElement)listView), (LibraryView.Detailed, detailedView), (LibraryView.Bookshelf, view) })
            {
                view.Visibility = listView.Visibility = detailedView.Visibility = Visibility.Collapsed;
                control.Visibility = Visibility.Visible;
                vm.SelectedView = mode;
                await IdleAsync();
                Require(expected.SetEquals(vm.SelectedBooks.Select(row => row.Id)), "Switching library views lost the shared selected IDs.");
                if (mode != LibraryView.Bookshelf)
                {
                    var grid = (Syncfusion.UI.Xaml.Grid.SfDataGrid)((UserControl)control).FindName("BooksGrid");
                    Require(expected.SetEquals(grid.SelectedItems.OfType<BookRowViewModel>().Select(row => row.Id)), "Grid did not restore the shared selection.");
                }
            }
            Console.WriteLine("PASS: shared selection is restored when switching through list, detailed and bookshelf views.");
        }
        finally { window.Close(); }
    }
    private static Task IdleAsync() => Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void SendKey(UIElement target, Key key) => target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,
        PresentationSource.FromVisual(target)!, Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
    private static IEnumerable<ListBoxItem> Containers(DependencyObject root)
    {
        if (root is ListBoxItem { DataContext: BookRowViewModel } item) yield return item;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            foreach (var child in Containers(VisualTreeHelper.GetChild(root, index))) yield return child;
    }
}

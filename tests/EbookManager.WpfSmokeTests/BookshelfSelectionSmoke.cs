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
        var otherInput = new TextBox();
        var initialHost = new DockPanel();
        DockPanel.SetDock(otherInput, Dock.Top);
        initialHost.Children.Add(otherInput); initialHost.Children.Add(view);
        var window = new Window { Content = initialHost, Width = 800, Height = 600 };
        try
        {
            window.Show(); window.Activate();
            await IdleAsync();
            var first = Containers(view).First(item => item.DataContext is BookRowViewModel row && row.Id == books[0].Id);
            first.Focus();
            Require(first.IsSelected == ((BookRowViewModel)first.DataContext).IsSelected, "Native tile selection diverged from the shared selection when focusing a tile.");
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
            SendKey((UIElement)Keyboard.FocusedElement, Key.Home);
            otherInput.Focus();
            await IdleAsync();
            Require(otherInput.IsKeyboardFocusWithin, "Delayed tile navigation stole focus from a newer text input.");
            Console.WriteLine("PASS: delayed tile focus respects a newer text-input focus.");
            await VerifyVerticalNavigationAsync(window, view, vm);
            vm.SelectBookshelfBook(vm.VisibleBooks[1], true, false);
            vm.SelectBookshelfBook(vm.VisibleBooks[5], false, true);
            await IdleAsync();
            Require(vm.SelectedBookCount == 5 && vm.SelectedBooks.All(row => row.IsSelected), "Tile range did not match the central bulk selection.");
            if (Environment.GetEnvironmentVariable("SAGA_WPF_CAPTURE_BOOKSHELF") is { Length: > 0 } capturePath)
            {
                var image = new System.Windows.Media.Imaging.RenderTargetBitmap((int)view.ActualWidth, (int)view.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                var background = new DrawingVisual();
                using (var drawing = background.RenderOpen()) drawing.DrawRectangle(Brushes.White, null, new Rect(0, 0, view.ActualWidth, view.ActualHeight));
                image.Render(background);
                image.Render(view);
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
                using var output = System.IO.File.Create(capturePath);
                encoder.Save(output);
            }
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
            initialHost.Children.Remove(view);
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
                    if (mode == LibraryView.List)
                    {
                        window.Activate();
                        await IdleAsync();
                        Require(FocusChild(grid) && grid.IsKeyboardFocusWithin, "The native grid must have a focused input element before testing user selection.");
                        grid.SelectedItems.Clear();
                        grid.SelectedItems.Add(vm.VisibleBooks[4]); grid.SelectedItems.Add(vm.VisibleBooks[7]);
                        await IdleAsync();
                        expected = new HashSet<Guid> { vm.VisibleBooks[4].Id, vm.VisibleBooks[7].Id };
                        Require(expected.SetEquals(vm.SelectedBooks.Select(row => row.Id)), "Native grid selection was not recorded for the bookshelf.");
                    }
                }
            }
            Console.WriteLine("PASS: shared selection is restored when switching through list, detailed and bookshelf views.");
        }
        finally { window.Close(); }
    }
    private static Task IdleAsync() => Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;
    private static async Task VerifyVerticalNavigationAsync(Window window, BookshelfView view, LibraryViewModel vm)
    {
        var list = (ListBox)view.FindName("BookshelfBooksList");
        var panel = FindPanel(list) ?? throw new InvalidOperationException("The tile wrap panel was not realized.");
        var originalWidth = window.Width;
        var originalPadding = list.Padding;
        var originalItemWidth = panel.ItemWidth;
        try
        {
            foreach (var (width, padding, itemWidth) in new[] { (700d, 0d, 170d), (870d, 0d, 170d), (800d, 64d, 170d), (800d, 0d, 200d) })
            {
                window.Width = width;
                list.Padding = new Thickness(padding, 0, padding, 0);
                panel.ItemWidth = itemWidth;
                await IdleAsync();
                list.ScrollIntoView(vm.VisibleBooks[0]);
                list.UpdateLayout();
                await IdleAsync();
                var first = Containers(list).Single(item => ((BookRowViewModel)item.DataContext).Id == vm.VisibleBooks[0].Id);
                var position = first.TransformToAncestor(list).Transform(new Point());
                // Derive the expected book from rendered positions, independently of the navigation calculation.
                var below = Containers(list).Select(item => new { Item = item, Position = item.TransformToAncestor(list).Transform(new Point()) })
                    .Where(item => item.Position.Y > position.Y + 1)
                    .OrderBy(item => item.Position.Y).ThenBy(item => Math.Abs(item.Position.X - position.X)).First().Item;
                var expected = ((BookRowViewModel)below.DataContext).Id;
                window.Activate(); first.Focus();
                SendKey(first, Key.Down);
                await IdleAsync();
                Require(vm.SelectedBook?.Id == expected,
                    $"Down did not follow the rendered tile column (window {width}, padding {padding}, tile {itemWidth}).");
                SendKey((UIElement)Keyboard.FocusedElement, Key.Up);
                await IdleAsync();
                Require(vm.SelectedBook?.Id == ((BookRowViewModel)first.DataContext).Id, "Up did not return to the original rendered column.");
            }
            Console.WriteLine("PASS: vertical tile navigation follows rendered columns across scrollbars, padding and item widths.");
        }
        finally
        {
            window.Width = originalWidth; list.Padding = originalPadding; panel.ItemWidth = originalItemWidth;
            await IdleAsync();
        }
    }

    private static EbookManager.App.Controls.VirtualizingWrapPanel? FindPanel(DependencyObject root)
    {
        if (root is EbookManager.App.Controls.VirtualizingWrapPanel panel) return panel;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            if (FindPanel(VisualTreeHelper.GetChild(root, index)) is { } found) return found;
        return null;
    }
    private static bool FocusChild(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is UIElement { Focusable: true, IsVisible: true, IsEnabled: true } target && target.Focus()) return true;
            if (FocusChild(child)) return true;
        }
        return false;
    }
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

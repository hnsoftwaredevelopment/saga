using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using EbookManager.App.Controls;
using EbookManager.App.Views;
using EbookManager.Application.Books;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;
using EbookManager.Infrastructure.Files;
using EbookManager.Infrastructure.Metadata;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.WpfSmokeTests;

internal static class GroupedBookSelectionSmoke
{
    public static async Task RunAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var books = Enumerable.Range(0, 8).Select(index => new Book(Guid.NewGuid(),
            new BookMetadata($"Grouped {index}", ["Author"], Tags: index == 1 ? ["First", "Second"] : [index < 4 ? "First" : "Second"]),
            ReadingStatus.Unread, null, now, now)).ToArray();
        var repository = new SmokeBookRepository(books);
        var service = new BookService(repository, new ManagedLibraryFileStore(System.IO.Path.GetTempPath()), new MetadataAdapterResolver([new FallbackMetadataAdapter()]));
        var vm = new LibraryViewModel(repository, new BookSearchService(), new BookDetailsViewModel(service), new SmokeInteraction(),
            bookService: service, searchFilterDelay: _ => Task.CompletedTask);
        await vm.RefreshAsync();
        var window = new Window { Width = 1000, Height = 700 };
        try
        {
            window.Show(); window.Activate();
            foreach (var mode in new[] { LibraryView.List, LibraryView.Detailed })
            {
                vm.SelectedView = mode;
                vm.SetGroupingOptions([LibraryGroupOption.Tag]);
                foreach (var group in vm.GroupedLibraryNodes) group.IsExpanded = true;
                UserControl view = mode == LibraryView.List ? new LibraryListView() : new DetailedGridView();
                view.DataContext = vm;
                var search = new TextBox();
                var host = new DockPanel();
                DockPanel.SetDock(search, Dock.Top);
                host.Children.Add(search); host.Children.Add(view);
                vm.SetSelectedBooks([vm.VisibleBooks[1], vm.VisibleBooks[5]]);
                window.Content = host;
                await IdleAsync();
                window.Activate();
                Require(search.Focus() && search.IsKeyboardFocusWithin, "The test window must own keyboard focus before exercising modifier input.");

                void Click(int index, ModifierKeys modifiers = ModifierKeys.None)
                {
                    var row = Descendants<LibraryColumnRowGrid>(view).First(grid => grid.BookRow?.Id == books[index].Id);
                    WithModifiers(modifiers, () =>
                    {
                        var click = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                            { RoutedEvent = Mouse.PreviewMouseDownEvent };
                        row.RaiseEvent(click);
                        // Match WPF's preview/bubble pair, including its shared Handled state.
                        click.RoutedEvent = Mouse.MouseDownEvent;
                        row.RaiseEvent(click);
                    });
                }
                void Selected(params int[] indices)
                {
                    Require(indices.Select(index => books[index].Id).ToHashSet().SetEquals(vm.SelectedBooks.Select(row => row.Id)),
                        $"{mode}: expected [{string.Join(",", indices)}], selected [{string.Join(",", vm.SelectedBooks.Select(row => row.Title))}].");
                    foreach (var item in Descendants<ListBoxItem>(view).Where(item => item.DataContext is BookRowViewModel))
                    {
                        Require(item.IsSelected == ((BookRowViewModel)item.DataContext).IsSelected, $"{mode}: grouped selection is not visible on the correct rows.");
                        if (((BookRowViewModel)item.DataContext).IsSelected)
                        {
                            var border = (Border)item.Template.FindName("SelectionBorder", item);
                            Require(Equals(border.BorderBrush, item.FindResource("AccentBrush")), $"{mode}: selected row has no accent border.");
                        }
                    }
                }
                void PressKey(Key key, ModifierKeys modifiers = ModifierKeys.None)
                {
                    var target = Keyboard.FocusedElement as UIElement ?? throw new InvalidOperationException("No row focus.");
                    WithModifiers(modifiers, () => target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,
                        PresentationSource.FromVisual(target), Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent }));
                }

                Selected(1, 5);
                Require(Descendants<ListBoxItem>(view).Count(item => item.DataContext is BookRowViewModel row && row.Id == books[1].Id) == 2,
                    "Duplicate tag fixture must render both occurrences of one book.");
                Click(1); Click(5, ModifierKeys.Control);
                Selected(1, 5);
                Click(5, ModifierKeys.Control); Selected(1);
                Click(5, ModifierKeys.Shift); Selected(5); // Ctrl toggle established a new anchor at 5.
                Click(1); Click(5, ModifierKeys.Shift); Selected(1, 2, 3, 4, 5);
                if (Environment.GetEnvironmentVariable("SAGA_WPF_CAPTURE_GROUPED") is { Length: > 0 } directory)
                {
                    view.UpdateLayout();
                    var image = new System.Windows.Media.Imaging.RenderTargetBitmap((int)view.ActualWidth, (int)view.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    image.Render(view);
                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
                    using var output = System.IO.File.Create(System.IO.Path.Combine(directory, $"grouped-{mode}-selection.png"));
                    encoder.Save(output);
                }
                Click(7, ModifierKeys.Control | ModifierKeys.Shift); Selected(1, 2, 3, 4, 5, 6, 7);
                Click(3); PressKey(Key.Down, ModifierKeys.Shift);
                await IdleAsync(); Selected(3, 4);
                Require((Keyboard.FocusedElement as FrameworkElement)?.DataContext is BookRowViewModel focus && focus.Id == books[4].Id,
                    $"{mode}: keyboard did not cross the group boundary.");
                PressKey(Key.Down, ModifierKeys.Control); await IdleAsync(); Selected(3, 4);
                PressKey(Key.Space, ModifierKeys.Control); Selected(3, 4, 5);
                PressKey(Key.Escape); Selected();
                Click(0); PressKey(Key.End); await IdleAsync(); Selected(7);
                PressKey(Key.Home); await IdleAsync(); Selected(0);
                PressKey(Key.End); search.Focus(); await IdleAsync();
                Require(search.IsKeyboardFocusWithin, $"{mode}: delayed navigation stole newer text focus.");
                Click(0);
                vm.SearchText = "Grouped 7"; await IdleAsync(); Selected();
                vm.SearchText = ""; await IdleAsync(); Selected();
                vm.SetSelectedBooks([vm.VisibleBooks[1], vm.VisibleBooks[5]]);
                vm.SelectedView = LibraryView.Bookshelf;
                vm.SetGroupingOptions([LibraryGroupOption.Author]);
                vm.SelectedView = mode;
                foreach (var group in vm.GroupedLibraryNodes) group.IsExpanded = true;
                await IdleAsync(); Selected(1, 5);
                Require(vm.ActiveGroupOptions.SequenceEqual([LibraryGroupOption.Tag]), $"{mode}: view switching replaced this view's own grouping.");
                Console.WriteLine($"PASS: grouped {mode} mouse/keyboard selection spans groups, reflects shared IDs and respects filters.");
            }
        }
        finally { window.Close(); }
    }

    // SetKeyboardState affects only this thread, never sends keys to the user's apps.
    // https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-setkeyboardstate
    internal static void WithModifiers(ModifierKeys modifiers, Action action)
    {
        var saved = new byte[256];
        Require(GetKeyboardState(saved), "Could not save test keyboard state.");
        var state = (byte[])saved.Clone();
        foreach (var key in new[] { 0x10, 0x11, 0x12, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0x5B, 0x5C }) state[key] = 0;
        if (modifiers.HasFlag(ModifierKeys.Control)) state[0x11] = state[0xA2] = 0x80;
        if (modifiers.HasFlag(ModifierKeys.Shift)) state[0x10] = state[0xA0] = 0x80;
        try
        {
            Require(SetKeyboardState(state), "Could not set test keyboard state.");
            Require(Keyboard.Modifiers == modifiers, $"Synthetic modifier state was not observed by WPF (expected {modifiers}, actual {Keyboard.Modifiers}).");
            action();
        }
        finally { Require(SetKeyboardState(saved), "Could not restore test keyboard state."); }
    }
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetKeyboardState([Out] byte[] state);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetKeyboardState(byte[] state);
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            foreach (var child in Descendants<T>(VisualTreeHelper.GetChild(root, index))) yield return child;
    }
    private static Task IdleAsync() => Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

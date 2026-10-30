using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using EbookManager.App.Views;
using EbookManager.Application.Tags;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Tags;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.WpfSmokeTests;

internal static class TagSelectionActionsSmoke
{
    public static async Task RunAsync()
    {
        var state = new TagState();
        using var vm = new TagManagementViewModel(state, state, key => EbookManager.App.Localization.LocalizedStrings.Current[key], state);
        var window = new TagManagementWindow(vm) { Width = 1000, Height = 650 };
        try
        {
            window.Show(); window.Activate(); await IdleAsync();
            await vm.SearchAsync(""); await vm.SelectAsync(state.Source); await IdleAsync();
            var grid = (DataGrid)window.FindName("TagBooksGrid");
            var change = (Button)window.FindName("ChangeSelectedTagButton");
            var remove = (Button)window.FindName("RemoveSelectedTagButton");
            Require(!change.IsEnabled && !remove.IsEnabled, "No selection must not mean all books.");
            grid.SelectedItems.Add(vm.Books.First()); await IdleAsync();
            Require(vm.SelectedBookCount == 1 && change.IsEnabled && remove.IsEnabled, "Native book selection did not enable scoped actions.");

            async Task Apply(Button button, bool confirm, string? target = null)
            {
                button.Focus();
                var complete = CompleteDialogAsync(confirm, target);
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await complete;
                await WaitAsync(() => !vm.IsBusy); await IdleAsync();
            }
            await Apply(change, false, "Roman");
            Require(state.Applications == 0 && vm.SelectedBookCount == 1 && grid.SelectedItems.Count == 1, "Cancel changed data or lost selection.");
            Require(change.IsKeyboardFocusWithin, "Cancel did not restore the action button focus.");
            var cells = Cells(grid).Where(cell => cell.IsSelected).ToArray();
            Require(cells.Length > 0 && cells.All(cell => Equals(cell.BorderBrush, cell.FindResource("AccentBrush"))),
                "Selected books need a visible accent marker even while an action button has focus.");
            if (Environment.GetEnvironmentVariable("SAGA_WPF_CAPTURE_TAG_SELECTION") is { Length: > 0 } directory)
            {
                foreach (var theme in new[] { "Light", "Dark" })
                {
                    System.Windows.Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
                        { Source = new Uri($"pack://application:,,,/Saga;component/Resources/Styles/Themes/{theme}Theme.xaml") };
                    await IdleAsync(); Capture(window, System.IO.Path.Combine(directory, $"tag-selection-{theme}.png"));
                }
                System.Windows.Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
                    { Source = new Uri("pack://application:,,,/Saga;component/Resources/Styles/Themes/LightTheme.xaml") };
            }
            await Apply(change, true, "Roman");
            Require(vm.TotalBooks == 1 && grid.Items.Count == 1 && vm.SelectedBookCount == 0, "Changed book did not disappear from the source list.");
            Require(vm.Tags.Single(tag => tag.Id == state.TargetId).BookCount == 2, "Existing target was duplicated or its count is stale.");
            grid.SelectedItems.Add(vm.Books.Single()); await IdleAsync();
            await Apply(remove, true);
            Require(vm.TagId is null && vm.Books.Count == 0 && grid.Items.Count == 0 && vm.Tags.Count == 1, "Removing the last source membership did not clear the pane/index.");
            Require(state.BookCount == 3 && state.Summary(state.TargetId).BookCount == 2, "Removing a tag deleted books or other tags.");
            Console.WriteLine("PASS: tag selection actions preserve cancel, deduplicate an existing tag and refresh both lists/counts.");

            state.AddSourceBooks(205);
            await vm.SearchAsync(""); await vm.SelectAsync(state.Source); await IdleAsync();
            grid.SelectedItems.Add(vm.Books[10]); await IdleAsync();
            var selected = vm.SelectedBookIds.Single();
            var more = (Button)window.FindName("MoreTagBooksButton");
            more.Focus(); more.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await WaitAsync(() => !vm.IsBusy && vm.Books.Count == 205); await IdleAsync();
            Require(vm.SelectedBookCount == 1 && grid.SelectedItems.OfType<TagBook>().Single().Id == selected, "More books lost or visually changed the selected IDs.");
            grid.SelectAll(); await IdleAsync();
            Require(vm.SelectedBookCount == 205, "Native Select All did not select exactly the loaded book rows.");
            Console.WriteLine("PASS: native tag book selection survives paging and Select All uses the loaded rows.");
        }
        finally
        {
            window.Close();
            System.Windows.Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
                { Source = new Uri("pack://application:,,,/Saga;component/Resources/Styles/Themes/LightTheme.xaml") };
        }
    }

    private static async Task CompleteDialogAsync(bool confirm, string? target)
    {
        await WaitAsync(() => System.Windows.Application.Current.Windows.OfType<TagAssignmentWindow>().Any(window => window.IsLoaded));
        var dialog = System.Windows.Application.Current.Windows.OfType<TagAssignmentWindow>().Single();
        try
        {
            await Task.Delay(50);
            var input = (TextBox)dialog.FindName("NewNameBox");
            var apply = (Button)dialog.FindName("ApplyButton");
            Require(dialog.IsActive && (target is null ? apply.IsKeyboardFocusWithin : input.IsKeyboardFocusWithin), "Selection dialog lost its intended input focus.");
            if (target is not null) input.Text = target;
            if (confirm)
            {
                Require(apply.IsEnabled, "Confirmed tag selection action is unavailable.");
                apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            else dialog.DialogResult = false;
        }
        finally { if (dialog.IsVisible) dialog.Close(); }
    }
    private static void Capture(Window window, string path)
    {
        window.UpdateLayout();
        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using var output = System.IO.File.Create(path); encoder.Save(output);
    }
    private static IEnumerable<DataGridCell> Cells(DependencyObject root)
    {
        if (root is DataGridCell cell) yield return cell;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            foreach (var child in Cells(VisualTreeHelper.GetChild(root, index))) yield return child;
    }
    private static Task IdleAsync() => Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;
    private static async Task WaitAsync(Func<bool> complete)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!complete()) { if (DateTime.UtcNow > deadline) throw new TimeoutException("Tag selection WPF operation did not complete."); await Task.Delay(10); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private sealed class TagState : ITagRepository, ITagRenameService, ITagAssignmentService
    {
        private readonly Guid sourceId = Guid.NewGuid();
        public Guid TargetId { get; } = Guid.NewGuid();
        private readonly Dictionary<Guid, string> names = [];
        private readonly Dictionary<Guid, (string Title, HashSet<Guid> Tags)> books = [];
        public int Applications { get; private set; }
        public int BookCount => books.Count;
        public TagSummary Source => Summary(sourceId);
        public TagState()
        {
            names.Add(sourceId, "<Geen tag>"); names.Add(TargetId, "Roman");
            books.Add(Guid.NewGuid(), ("First", [sourceId, TargetId]));
            books.Add(Guid.NewGuid(), ("Second", [sourceId]));
            books.Add(Guid.NewGuid(), ("Other", [TargetId]));
        }
        public void AddSourceBooks(int count) { for (var index = 0; index < count; index++) books.Add(Guid.NewGuid(), ($"Book {index:D3}", [sourceId])); }
        public TagSummary Summary(Guid id) => new(id, names[id], books.Count(book => book.Value.Tags.Contains(id)));
        public Task<TagPage> SearchAsync(string query, int skip, int take, CancellationToken token)
        {
            var rows = names.Keys.Select(Summary).Where(tag => tag.BookCount > 0 && tag.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
            return Task.FromResult(new TagPage(rows.Skip(skip).Take(take).ToArray(), rows.Length));
        }
        public async Task<TagBookPage> ListBooksAsync(Guid id, int skip, int take, CancellationToken token)
        {
            await Task.Delay(20, token);
            var rows = books.Where(book => book.Value.Tags.Contains(id)).Select(book => new TagBook(book.Key, book.Value.Title, "Author")).ToArray();
            return new(rows.Skip(skip).Take(take).ToArray(), rows.Length);
        }
        public async Task<TagAssignmentPreview> PreviewAsync(Guid id, IReadOnlyCollection<Guid> ids, CancellationToken token)
        {
            await Task.Delay(20, token);
            return new(id, names[id], ids.Select(book => new TagSourceBook(book, books[book].Title)).ToArray());
        }
        public Task<TagAssignmentCompletion> ApplyAsync(TagAssignmentPreview preview, TagAssignmentRequest request, CancellationToken token)
        {
            Applications++;
            foreach (var book in preview.Books)
            {
                books[book.Id].Tags.Remove(preview.TagId);
                if (request.Operation == TagAssignmentOperation.Replace) books[book.Id].Tags.Add(TargetId);
            }
            return Task.FromResult(new TagAssignmentCompletion(new(TagAssignmentStatus.Applied, preview.TagId,
                request.Operation == TagAssignmentOperation.Replace ? TargetId : null, request.TargetName, preview.Books), []));
        }
        public Task<TagRenamePreview> PreviewRenameAsync(Guid id, CancellationToken token) => throw new NotSupportedException();
        public Task<TagRenamePreview> PreviewAsync(Guid id, CancellationToken token) => throw new NotSupportedException();
        public Task<TagRenameResult> RenameAsync(TagRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
        Task<TagRenameCompletion> ITagRenameService.RenameAsync(TagRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
    }
}

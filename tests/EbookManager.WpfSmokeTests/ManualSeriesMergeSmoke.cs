using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EbookManager.App.Localization;
using EbookManager.App.Views;
using EbookManager.Application.Series;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Series;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.WpfSmokeTests;

internal static class ManualSeriesMergeSmoke
{
    public static async Task RunAsync()
    {
        var store = new Store(); var numbers = store.Rows.ToDictionary(row => row.Id, row => row.Number);
        using var vm = new SeriesManagementViewModel(store, store, Text, store);
        var window = new SeriesManagementWindow(vm);
        try
        {
            window.Show(); window.Activate(); await WaitAsync(() => vm.Series.Count == 200 && !vm.IsWorking);
            var list = (ListBox)window.FindName("SeriesList"); var merge = (Button)window.FindName("MergeSeriesButton");
            Require(list.SelectionMode == SelectionMode.Extended && !merge.IsEnabled, "Manual merge must require native multi-selection.");
            list.SelectedIndex = 0; await WaitAsync(() => !vm.IsBusy && vm.TotalBooks == 1); await IdleAsync();
            list.SelectedItems.Add(vm.Series[1]); list.SelectedItems.Add(vm.Series[2]); await IdleAsync();
            Require(vm.MergeSeriesCount == 3 && merge.IsEnabled && !vm.CanRename && !((DataGrid)window.FindName("BooksGrid")).IsEnabled, "Multi-series selection must enable merge and block edits to a single series.");
            await vm.SearchAsync("", true); await IdleAsync();
            Require(vm.Series.Count == 205 && list.SelectedItems.Count == 3, "Load more lost selected series.");
            foreach (var mode in new[] { "cancel", "existing", "new" })
            {
                if (mode == "new") { list.SelectedItems.Add(vm.Series.Single(row => row.Name == "Series 004")); await IdleAsync(); }
                merge.Focus(); var dialog = CompleteDialogAsync(mode);
                merge.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await dialog;
                await WaitAsync(() => !vm.IsWorking); await IdleAsync();
                if (mode == "cancel") Require(store.Saves == 0 && list.SelectedItems.Count == 3 && merge.IsKeyboardFocusWithin, "Cancel changed data, selection or focus.");
                else
                {
                    Require(vm.Query == "" && vm.MergeSeriesCount == 1 && list.SelectedItems.Count == 1 && list.IsKeyboardFocusWithin && !merge.IsEnabled, "Successful manual merge must select/focus the target and clear the query.");
                    Require(vm.TotalBooks == (mode == "existing" ? 3 : 4) && !vm.IsError && vm.CanBrowseBooks, "Target books or edit availability did not refresh.");
                    Require(vm.Name == (mode == "existing" ? "Series 002" : "Nieuwe reeks"), "The confirmed target name was not used.");
                }
            }
            Require(store.Saves == 2 && vm.ChangedBookIds.Count == 4 && store.Rows.All(row => numbers[row.Id] == row.Number), "Merging changed numbers or lost changed book IDs.");
            list.SelectedItems.Add(vm.Series.First(row => row.Name != vm.Name)); await IdleAsync();
            var search = (TextBox)window.FindName("SearchBox"); search.Focus(); search.Text = "No match";
            await WaitAsync(() => vm.Series.Count == 0 && !vm.IsWorking); await IdleAsync();
            Require(vm.MergeSeriesCount == 0 && !merge.IsEnabled && search.IsKeyboardFocusWithin, "New searches must clear hidden merge selection without stealing focus.");
            Console.WriteLine("PASS: manual series merge preserves native multi-selection/paging/cancel, typing focus and numbers; existing/new targets refresh selection, counts and query.");
        }
        finally { window.Close(); }
    }
    private static async Task CompleteDialogAsync(string mode)
    {
        await WaitAsync(() => System.Windows.Application.Current.Windows.OfType<SeriesMergeWindow>().Any(window => window.IsLoaded));
        var window = System.Windows.Application.Current.Windows.OfType<SeriesMergeWindow>().Single();
        try
        {
            await IdleAsync(); var vm = (SeriesMergeDialogViewModel)window.DataContext;
            var existing = (ComboBox)window.FindName("TargetBox"); var input = (TextBox)window.FindName("NewNameBox"); var confirm = (Button)window.FindName("MergeButton");
            Require(vm.CanUseNewName && input.IsVisible && !confirm.IsEnabled && existing.IsKeyboardFocusWithin, "Manual comparison requires an explicit existing/new target.");
            Require(vm.Books.Count == (mode == "new" ? 4 : 3), "Comparison must contain every book from the selected series.");
            input.Focus();
            foreach (var character in "Nieuwe reeks") { input.AppendText(character.ToString()); await IdleAsync(); Require(input.IsKeyboardFocusWithin, "Typing a new series name lost focus."); }
            Require(confirm.IsEnabled && vm.TargetName == "Nieuwe reeks" && existing.SelectedItem is null, "Typing did not select the new name exclusively.");
            if (mode == "existing")
            {
                existing.SelectedItem = vm.Targets.Single(row => row.Name == "Series 002"); await IdleAsync();
                Require(input.Text == "" && vm.TargetName == "Series 002" && confirm.IsEnabled, "Choosing an existing name did not clear the new-name alternative.");
            }
            if (mode == "cancel") { window.Close(); return; }
            if (mode == "new")
            {
                window.Width = window.MinWidth; window.Height = window.MinHeight; window.UpdateLayout(); await IdleAsync();
                Require(confirm.TranslatePoint(new Point(), window).Y + confirm.ActualHeight < window.ActualHeight, "Merge action disappeared at minimum size.");
                if (Environment.GetEnvironmentVariable("SAGA_WPF_CAPTURE_MANUAL_SERIES_MERGE") is { Length: > 0 } capture)
                {
                    Directory.CreateDirectory(capture);
                    foreach (var theme in new[] { "Light", "Dark" })
                    {
                        System.Windows.Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary { Source = new Uri($"pack://application:,,,/Saga;component/Resources/Styles/Themes/{theme}Theme.xaml") };
                        window.UpdateLayout(); await IdleAsync();
                        var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(window);
                        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                        using var stream = File.Create(Path.Combine(capture, $"manual-series-merge-{theme}.png")); encoder.Save(stream);
                    }
                }
            }
            confirm.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
        finally { if (window.IsVisible) window.Close(); }
    }
    private static string Text(string key) => LocalizedStrings.Current[key];
    private static async Task IdleAsync() => await Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
    private static async Task WaitAsync(Func<bool> condition)
    {
        var until = DateTime.UtcNow.AddSeconds(15);
        while (!condition()) { if (DateTime.UtcNow > until) throw new TimeoutException("Manual series merge UI did not settle."); await Task.Delay(20); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class Store : ISeriesRepository, ISeriesRenameService, ISeriesMergeService
    {
        public List<SeriesMergeBook> Rows = Enumerable.Range(1,205).Select(i => new SeriesMergeBook(Guid.NewGuid(), $"Series {i:000}", $"Book {i:000}", "Author", i == 3 ? null : i % 2)).ToList();
        public int Saves;
        public Task<SeriesPage> SearchAsync(string query, int skip, int take, CancellationToken token)
        { var all = Rows.GroupBy(row => row.StoredName).Select(group => new SeriesSummary(group.Key, group.Count())).Where(row => row.Name.Contains(query)).OrderBy(row => row.Name).ToArray(); return Task.FromResult(new SeriesPage(all.Skip(skip).Take(take).ToArray(), all.Length)); }
        public Task<SeriesBookPage> ListBooksAsync(string name, int skip, int take, CancellationToken token)
        { var all = Rows.Where(row => row.StoredName == name).OrderBy(row => row.Number is null).ThenBy(row => row.Number).ToArray(); return Task.FromResult(new SeriesBookPage(all.Skip(skip).Take(take).Select(row => new SeriesBook(row.Id, row.Title, row.Authors, row.Number)).ToArray(), all.Length)); }
        public Task<SeriesMergePreview?> PreviewAsync(IReadOnlyCollection<string> names, CancellationToken token) => Task.FromResult<SeriesMergePreview?>(new(names.Select(name => new SeriesMergeGroup(name, Rows.Where(row => row.StoredName == name).ToArray())).ToArray()));
        public Task<SeriesMergeCompletion> MergeAsync(SeriesMergePreview preview, string target, CancellationToken token)
        {
            Saves++; var changed = preview.Groups.SelectMany(group => group.Books).Where(row => row.StoredName != target).ToArray(); var ids = changed.Select(row => row.Id).ToHashSet();
            Rows = Rows.Select(row => ids.Contains(row.Id) ? row with { StoredName = target } : row).ToList();
            return Task.FromResult(new SeriesMergeCompletion(new(SeriesMergeStatus.Merged, target, changed), []));
        }
        public Task<SeriesRenamePreview> PreviewAsync(string name, CancellationToken token) => throw new NotSupportedException();
        public Task<SeriesRenamePreview> PreviewRenameAsync(string name, CancellationToken token) => throw new NotSupportedException();
        public Task<SeriesRenameResult> RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
        Task<SeriesRenameCompletion> ISeriesRenameService.RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
    }
}

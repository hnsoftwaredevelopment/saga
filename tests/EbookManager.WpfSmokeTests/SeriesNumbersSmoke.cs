using System.Globalization;
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

internal static class SeriesNumbersSmoke
{
    public static async Task RunAsync()
    {
        var store = new Store();
        using var vm = new SeriesManagementViewModel(store, store, Text, numbersService: store);
        var window = new SeriesManagementWindow(vm);
        try
        {
            window.Show(); window.Activate(); await WaitAsync(() => vm.Series.Count == 1 && !vm.IsWorking);
            ((ListBox)window.FindName("SeriesList")).SelectedIndex = 0;
            await WaitAsync(() => vm.Books.Count == 200 && !vm.IsBusy); await IdleAsync();
            var grid = (DataGrid)window.FindName("BooksGrid"); var assign = (Button)window.FindName("AssignNumbersButton");
            Require(grid.SelectionMode == DataGridSelectionMode.Extended && !assign.IsEnabled, "Series book list must support native extended selection and require multiple books.");
            grid.SelectedItems.Add(vm.Books.Single(row => row.Title == "Book 001"));
            grid.SelectedItems.Add(vm.Books.Single(row => row.Title == "Book 002")); await IdleAsync();
            Require(vm.SelectedNumberBooks.Count == 2 && assign.IsEnabled, "Selecting two books did not enable series numbering.");
            await vm.LoadMoreBooksAsync(); await IdleAsync();
            Require(vm.Books.Count == 205 && grid.SelectedItems.Count == 2 && vm.SelectedNumberBooks.Count == 2, "Load more lost the selected series books.");
            foreach (var save in new[] { false, true })
            {
                assign.Focus(); var dialog = CompleteDialogAsync(save);
                assign.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await dialog;
                await WaitAsync(() => !vm.IsBusy); await IdleAsync();
                Require(grid.SelectedItems.Count == 2 && vm.SelectedNumberBooks.Count == 2 && assign.IsKeyboardFocusWithin, "Numbering dialog lost parent selection or caller focus.");
                Require(store.Saves == (save ? 1 : 0), "Cancelling wrote values or saving did not run once.");
                if (save)
                {
                    Require(vm.Books.Single(row => row.Title == "Book 001").Number == 2.5m && vm.Books.Single(row => row.Title == "Book 002").Number == 1.5m, "The visible reordered proposal was not saved.");
                    Require(vm.ChangedBookIds.Count == 2 && !vm.IsError && vm.StatusMessage.Contains(Text("SeriesNumbersDuplicates").Split('{')[0]), "Saved IDs or informational duplicate feedback are missing.");
                }
            }
            Console.WriteLine("PASS: selected series books survive paging, preview/cancel/reorder/decimal save, duplicate information and caller focus; single-number behavior remains available.");
        }
        finally { window.Close(); }
    }
    private static async Task CompleteDialogAsync(bool save)
    {
        await WaitAsync(() => System.Windows.Application.Current.Windows.OfType<SeriesNumbersWindow>().Any(window => window.IsLoaded));
        var window = System.Windows.Application.Current.Windows.OfType<SeriesNumbersWindow>().Single();
        try
        {
            await IdleAsync(); var vm = (SeriesNumbersDialogViewModel)window.DataContext;
            var input = (TextBox)window.FindName("StartBox"); var button = (Button)window.FindName("SaveNumbersButton");
            Require(input.IsKeyboardFocusWithin && vm.Rows.Count == 2, "Batch preview must focus start number and use exactly the parent selection.");
            input.Text = "letters"; await IdleAsync(); Require(!button.IsEnabled && input.IsKeyboardFocusWithin && vm.ValidationMessage.Length > 0, "Invalid start number must keep typing focus and block saving.");
            input.Text = 1.5m.ToString(CultureInfo.CurrentCulture); await IdleAsync();
            Require(button.IsEnabled && vm.DuplicateCount == 1 && vm.DuplicateMessage.Length > 0, "Decimals and duplicate-number information must not block confirmation.");
            var first = vm.SelectedRow;
            ((Button)window.FindName("MoveDownButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await IdleAsync();
            Require(ReferenceEquals(vm.SelectedRow, first) && ReferenceEquals(vm.Rows[1], first) && vm.Rows[0].ProposedNumber == 1.5m && vm.Rows[1].ProposedNumber == 2.5m, "Move down lost selection or failed to refresh proposed numbers.");
            window.Width = window.MinWidth; window.Height = window.MinHeight; window.UpdateLayout(); await IdleAsync();
            Require(button.TranslatePoint(new Point(), window).Y + button.ActualHeight < window.ActualHeight, "Number assignment button is outside minimum window bounds.");
            if (save && Environment.GetEnvironmentVariable("SAGA_WPF_CAPTURE_SERIES_NUMBERS") is { Length: > 0 } capture)
            {
                Directory.CreateDirectory(capture);
                foreach (var theme in new[] { "Light", "Dark" })
                {
                    System.Windows.Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary { Source = new Uri($"pack://application:,,,/Saga;component/Resources/Styles/Themes/{theme}Theme.xaml") };
                    window.UpdateLayout(); await IdleAsync();
                    var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(window);
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var stream = File.Create(Path.Combine(capture, $"series-numbers-{theme}.png")); encoder.Save(stream);
                }
            }
            ((Button)window.FindName(save ? "SaveNumbersButton" : "CancelButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if (!save) window.Close();
        }
        finally { if (window.IsVisible) window.Close(); }
    }
    private static string Text(string key) => LocalizedStrings.Current[key];
    private static async Task IdleAsync() => await Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
    private static async Task WaitAsync(Func<bool> condition)
    {
        var until = DateTime.UtcNow.AddSeconds(15);
        while (!condition()) { if (DateTime.UtcNow > until) throw new TimeoutException("Series numbering UI did not settle."); await Task.Delay(20); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class Store : ISeriesRepository, ISeriesRenameService, ISeriesNumbersService
    {
        private readonly List<SeriesBook> rows = Enumerable.Range(1,205).Select(i => new SeriesBook(Guid.NewGuid(), $"Book {i:000}", "Author", i == 3 ? 1.5m : i)).ToList();
        public int Saves;
        public Task<SeriesPage> SearchAsync(string query, int skip, int take, CancellationToken token) => Task.FromResult(new SeriesPage([new("Series", rows.Count)], 1));
        public Task<SeriesBookPage> ListBooksAsync(string name, int skip, int take, CancellationToken token) => Task.FromResult(new SeriesBookPage(rows.OrderBy(row => row.Number).ThenBy(row => row.Title).Skip(skip).Take(take).ToArray(), rows.Count));
        public Task<SeriesNumbersPreview?> PreviewAsync(string name, IReadOnlyList<Guid> ids, CancellationToken token) => Task.FromResult<SeriesNumbersPreview?>(new(name, ids.Select(id => new SeriesNumberSource(name, rows.Single(row => row.Id == id))).ToArray(), rows.Where(row => !ids.Contains(row.Id)).Select(row => row.Number!.Value).ToArray()));
        public Task<SeriesNumbersCompletion> ChangeAsync(SeriesNumbersPreview preview, IReadOnlyList<SeriesNumberAssignment> assignments, CancellationToken token)
        {
            Saves++;
            foreach (var item in assignments) { var index = rows.FindIndex(row => row.Id == item.Id); rows[index] = rows[index] with { Number = item.Number }; }
            return Task.FromResult(new SeriesNumbersCompletion(new(SeriesNumberStatus.Changed, preview.Books.Select(row => new SeriesSourceBook(row.Book.Id, row.StoredName, row.Book.Title)).ToArray(), 1), []));
        }
        public Task<SeriesRenamePreview> PreviewAsync(string name, CancellationToken token) => throw new NotSupportedException();
        public Task<SeriesRenamePreview> PreviewRenameAsync(string name, CancellationToken token) => throw new NotSupportedException();
        public Task<SeriesRenameResult> RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
        Task<SeriesRenameCompletion> ISeriesRenameService.RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
    }
}

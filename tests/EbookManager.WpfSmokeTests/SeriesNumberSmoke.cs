using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EbookManager.App.Views;
using EbookManager.Application.Series;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Series;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.WpfSmokeTests;

internal static class SeriesNumberSmoke
{
    public static async Task RunAsync()
    {
        var store = new Store();
        using var vm = new SeriesManagementViewModel(store, store, Text, numberService: store);
        var window = new SeriesManagementWindow(vm);
        try
        {
            window.Show(); window.Activate();
            await WaitAsync(() => !vm.IsWorking && vm.Series.Count == 1); await IdleAsync();
            var series = (ListBox)window.FindName("SeriesList"); series.SelectedIndex = 0;
            await WaitAsync(() => !vm.IsBusy && vm.Books.Count == 4); await IdleAsync();
            var grid = (DataGrid)window.FindName("BooksGrid"); var button = (Button)window.FindName("ChangeNumberButton");
            Require(!button.IsEnabled, "Number editing must require a loaded selected book.");
            grid.SelectedIndex = 0; await IdleAsync();
            Require(button.IsEnabled, "Selecting one book did not enable number editing.");
            var id = vm.SelectedBook!.Id;
            foreach (var action in new[] { "cancel", "decimal", "duplicate", "clear" })
            {
                button.Focus();
                var dialog = CompleteDialogAsync(action);
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await dialog; await WaitAsync(() => !vm.IsBusy); await IdleAsync();
                Require(button.IsKeyboardFocusWithin && vm.SelectedBook?.Id == id, "Number edit lost its book selection or caller focus.");
                var selected = vm.SelectedBook ?? throw new InvalidOperationException("Selected book was lost.");
                if (action == "cancel") Require(store.Saves == 0 && selected.Number == 1, "Cancel modified the book.");
                if (action == "decimal") Require(selected.Number == 2.5m && vm.Books[1].Id == id, "Saving a decimal did not update numeric ordering.");
                if (action == "duplicate") Require(selected.Number == 2 && vm.StatusMessage.Contains(Text("SeriesNumberDuplicate").Split('{')[0]), "Duplicate numbers must stay allowed and be explained after saving.");
                if (action == "clear") Require(selected.Number is null && vm.Books.Skip(2).Any(book => book.Id == id), "Clearing the number did not move the book after numbered books.");
                Require(!vm.IsError && vm.Warnings.Count == 0, "A successful number edit must not show an error or file warning.");
            }
            Require(store.Saves == 3 && vm.ChangedBookIds.SequenceEqual([id]) && vm.Name == "Series" && vm.TotalBooks == 4, "Number changes reported unrelated books or changed series identity/counts.");
            Console.WriteLine("PASS: series number editing preserves cancel/focus, accepts decimals and duplicates, clears numbers and immediately refreshes numeric order.");
        }
        finally { window.Close(); }
    }

    private static async Task CompleteDialogAsync(string action)
    {
        await WaitAsync(() => System.Windows.Application.Current.Windows.OfType<SeriesNumberWindow>().Any(window => window.IsLoaded));
        var window = System.Windows.Application.Current.Windows.OfType<SeriesNumberWindow>().Single();
        try
        {
            await IdleAsync();
            var vm = (SeriesNumberDialogViewModel)window.DataContext;
            var input = (TextBox)window.FindName("NumberBox"); var save = (Button)window.FindName("SaveNumberButton");
            Require(input.IsKeyboardFocusWithin && !save.IsEnabled, "Number editor must focus its current value and avoid unchanged saves.");
            input.Text = "letters"; await IdleAsync();
            Require(!save.IsEnabled && vm.ValidationMessage.Length > 0 && input.IsKeyboardFocusWithin, "Invalid input must be explained and preserve typing focus.");
            input.Text = action switch { "decimal" => 2.5m.ToString(CultureInfo.CurrentCulture), "duplicate" => "2", "clear" => "", _ => "8" };
            await IdleAsync(); Require(save.IsEnabled, "Valid changed numbers, including empty values, must enable saving.");
            if (action == "duplicate")
            {
                Require(vm.HasDuplicate && vm.DuplicateMessage.Length > 0, "Duplicate warning is missing.");
                window.Width = window.MinWidth; window.Height = window.MinHeight; window.UpdateLayout(); await IdleAsync();
                Require(save.IsVisible && save.TranslatePoint(new Point(), window).Y + save.ActualHeight < window.ActualHeight, "Number save action disappeared at minimum size.");
                if (Environment.GetEnvironmentVariable("SAGA_WPF_CAPTURE_SERIES_NUMBER") is { Length: > 0 } directory)
                {
                    Directory.CreateDirectory(directory);
                    foreach (var theme in new[] { "Light", "Dark" })
                    {
                        System.Windows.Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
                        { Source = new Uri($"pack://application:,,,/Saga;component/Resources/Styles/Themes/{theme}Theme.xaml") };
                        window.UpdateLayout(); await IdleAsync();
                        var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(window);
                        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                        using var stream = File.Create(Path.Combine(directory, $"series-number-{theme}.png")); encoder.Save(stream);
                    }
                }
            }
            var clicked = action == "cancel" ? (Button)window.FindName("CancelButton") : save;
            ((IInvokeProvider)new ButtonAutomationPeer(clicked).GetPattern(PatternInterface.Invoke)).Invoke();
            await WaitAsync(() => !window.IsVisible);
        }
        finally
        {
            if (window.IsVisible) window.Close();
            System.Windows.Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
            { Source = new Uri("pack://application:,,,/Saga;component/Resources/Styles/Themes/LightTheme.xaml") };
        }
    }
    private static string Text(string key) => EbookManager.App.Localization.LocalizedStrings.Current[key];
    private static Task IdleAsync() => Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;
    private static async Task WaitAsync(Func<bool> complete)
    { var deadline = DateTime.UtcNow.AddSeconds(10); while (!complete()) { if (DateTime.UtcNow > deadline) throw new TimeoutException("Series number editing timed out."); await Task.Delay(10); } }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private sealed class Store : ISeriesRepository, ISeriesRenameService, ISeriesNumberService
    {
        private readonly List<SeriesBook> books = [new(Guid.NewGuid(), "One", "Author", 1), new(Guid.NewGuid(), "Two", "Author", 2), new(Guid.NewGuid(), "Ten", "Other author", 10), new(Guid.NewGuid(), "Missing", "Other author", null)];
        public int Saves;
        public Task<SeriesPage> SearchAsync(string query, int skip, int take, CancellationToken token) => Task.FromResult(new SeriesPage([new("Series", books.Count)], 1));
        public Task<SeriesBookPage> ListBooksAsync(string name, int skip, int take, CancellationToken token) => Task.FromResult(new SeriesBookPage(books.OrderBy(book => book.Number is null).ThenBy(book => book.Number).ThenBy(book => book.Title).Skip(skip).Take(take).ToArray(), books.Count));
        public Task<SeriesNumberPreview?> PreviewAsync(Guid id, CancellationToken token) => Task.FromResult<SeriesNumberPreview?>(new("Series", books.Single(book => book.Id == id), books.Where(book => book.Id != id && book.Number.HasValue).Select(book => book.Number!.Value).ToArray()));
        public Task<SeriesNumberCompletion> ChangeAsync(SeriesNumberPreview preview, decimal? number, CancellationToken token)
        { var index = books.FindIndex(book => book.Id == preview.Book.Id); books[index] = books[index] with { Number = number }; Saves++; return Task.FromResult(new SeriesNumberCompletion(new(SeriesNumberStatus.Changed, number.HasValue ? books.Count(book => book.Id != preview.Book.Id && book.Number == number) : 0), false)); }
        public Task<SeriesRenamePreview> PreviewAsync(string name, CancellationToken token) => throw new NotSupportedException();
        public Task<SeriesRenamePreview> PreviewRenameAsync(string name, CancellationToken token) => throw new NotSupportedException();
        public Task<SeriesRenameResult> RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
        Task<SeriesRenameCompletion> ISeriesRenameService.RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
    }
}

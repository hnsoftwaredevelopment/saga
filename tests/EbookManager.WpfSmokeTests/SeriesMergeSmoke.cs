using System.IO;
using System.Windows;
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

internal static class SeriesMergeSmoke
{
    public static async Task RunAsync()
    {
        var state = new Store();
        using var manager = new SeriesManagementViewModel(state, state, Text, state);
        var root = new SeriesManagementWindow(manager);
        try
        {
            root.Show(); root.Activate();
            await WaitAsync(() => manager.Series.Count == 4 && !manager.IsWorking);
            var search = (TextBox)root.FindName("SearchBox");
            search.Text = "Bob-";
            await WaitAsync(() => manager.Series.Count == 1 && !manager.IsWorking); await IdleAsync();
            var button = (Button)root.FindName("FindDuplicatesButton");
            button.Focus();
            var interaction = CompleteDiscoveryAsync(state);
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await interaction; await IdleAsync();
            Require(manager.Query.Length == 0 && search.Text.Length == 0 && manager.Series.Count == 3,
                "Series manager did not clear its search and show the complete refreshed series index.");
            Require(manager.Name == "Bob Evers" && manager.TotalBooks == 205 && manager.Books.Count == 200 && manager.ChangedBookIds.Count == 203,
                "Series manager did not retain the merged target, first book page or changed metadata IDs.");
            Require(button.IsKeyboardFocusWithin, "Closing discovery did not return focus to its caller.");
            Console.WriteLine("PASS: series discovery and explicit comparison/merge refresh the manager and clear its filter while retaining target books and focus.");
        }
        finally { root.Close(); }
    }

    private static async Task CompleteDiscoveryAsync(Store state)
    {
        await WaitAsync(() => System.Windows.Application.Current.Windows.OfType<SeriesDuplicatesWindow>().Any(window => window.IsLoaded));
        var window = System.Windows.Application.Current.Windows.OfType<SeriesDuplicatesWindow>().Single();
        var vm = (SeriesDuplicatesViewModel)window.DataContext;
        try
        {
            await WaitAsync(() => !vm.IsWorking && vm.Candidates.Count == 1); await IdleAsync();
            var search = (TextBox)window.FindName("SearchBox");
            var grid = (DataGrid)window.FindName("CandidatesGrid");
            var compare = (Button)window.FindName("CompareButton");
            Require(state.Merges == 0 && !compare.IsEnabled, "Discovery must never merge automatically.");
            search.Focus(); search.Text = "absent";
            await WaitAsync(() => !vm.IsWorking && vm.Candidates.Count == 0); await IdleAsync();
            Require(search.IsKeyboardFocusWithin, "Series filtering stole typing focus.");
            search.Text = "Bob Evers";
            await WaitAsync(() => !vm.IsWorking && vm.Candidates.Count == 1); await IdleAsync();
            grid.SelectedIndex = 0; await IdleAsync();
            foreach (var confirm in new[] { false, true })
            {
                compare.Focus();
                var comparison = CompleteComparisonAsync(confirm);
                compare.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await comparison; await WaitAsync(() => !vm.IsWorking); await IdleAsync();
                if (!confirm) Require(state.Merges == 0 && vm.Selected is not null && compare.IsKeyboardFocusWithin,
                    "Cancel must preserve data, the selected suggestion and caller focus.");
            }
            Require(window.HasMerged && state.Merges == 1 && vm.Candidates.Count == 0 && vm.LastActionMessage.Length > 0,
                "Merged suggestion did not disappear or the save result was lost on refresh.");
            Console.WriteLine("PASS: series suggestions support filtering, preserve cancel/focus, distinguish numbered series and refresh after confirmation.");
        }
        finally { window.Close(); }
    }

    private static async Task CompleteComparisonAsync(bool confirm)
    {
        await WaitAsync(() => System.Windows.Application.Current.Windows.OfType<SeriesMergeWindow>().Any(window => window.IsLoaded));
        var window = System.Windows.Application.Current.Windows.OfType<SeriesMergeWindow>().Single();
        try
        {
            var vm = (SeriesMergeDialogViewModel)window.DataContext;
            var target = (ComboBox)window.FindName("TargetBox");
            var merge = (Button)window.FindName("MergeButton");
            await IdleAsync();
            Require(target.IsKeyboardFocusWithin && !target.IsEditable && !merge.IsEnabled, "Comparison must focus an explicit choice among existing names.");
            Require(vm.Books.Count == 200 && vm.Books.Take(2).All(book => book.Number == 1 && book.Note == Text("SeriesMergeRepeatedNumber")),
                "Comparison must show duplicate numbers without blocking or renumbering.");
            var more = (Button)window.FindName("MoreBooksButton");
            more.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await IdleAsync();
            Require(vm.Books.Count == 205 && vm.Books.Last().Number is null && vm.Books.Last().Note == Text("SeriesMergeMissingNumber"),
                "Comparison must expose all books, sorted numerically with missing numbers last.");
            target.SelectedIndex = 1; await IdleAsync();
            Require(merge.IsEnabled, "An existing series name must enable merge, including when numbers repeat.");
            window.Width = window.MinWidth; window.Height = window.MinHeight; window.UpdateLayout(); await IdleAsync();
            Require(merge.IsVisible && merge.TranslatePoint(new Point(), window).Y + merge.ActualHeight < window.ActualHeight,
                "Merge action disappeared at minimum window size.");
            if (confirm && Environment.GetEnvironmentVariable("SAGA_WPF_CAPTURE_SERIES") is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory);
                foreach (var theme in new[] { "Light", "Dark" })
                {
                    System.Windows.Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
                    { Source = new Uri($"pack://application:,,,/Saga;component/Resources/Styles/Themes/{theme}Theme.xaml") };
                    window.UpdateLayout(); await IdleAsync();
                    var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(window);
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var stream = File.Create(Path.Combine(directory, $"series-merge-{theme}.png")); encoder.Save(stream);
                }
            }
            if (confirm) merge.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); else window.DialogResult = false;
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
    { var deadline = DateTime.UtcNow.AddSeconds(10); while (!complete()) { if (DateTime.UtcNow > deadline) throw new TimeoutException("Series merge timed out."); await Task.Delay(10); } }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private sealed class Store : ISeriesRepository, ISeriesRenameService, ISeriesMergeService
    {
        private readonly SeriesMergePreview preview = new([
            new("Bob-Evers", Enumerable.Range(1, 203).Select(number => new SeriesMergeBook(Guid.NewGuid(), "Bob-Evers", $"Book {number}", "Author", number)).ToArray()),
            new("Bob Evers", [new(Guid.NewGuid(), "Bob Evers", "Repeated", "Other", 1), new(Guid.NewGuid(), "Bob Evers", "Missing", "Other", null)])]);
        public int Merges { get; private set; }
        public Task<SeriesPage> SearchAsync(string query, int skip, int take, CancellationToken token)
        {
            SeriesSummary[] rows = Merges == 0 ? [new("Bob-Evers", 203), new("Bob Evers", 2), new("Serie 1", 1), new("Serie 2", 1)] : [new("Bob Evers", 205), new("Serie 1", 1), new("Serie 2", 1)];
            rows = rows.Where(row => row.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
            return Task.FromResult(new SeriesPage(rows.Skip(skip).Take(take).ToArray(), rows.Length));
        }
        public Task<SeriesBookPage> ListBooksAsync(string name, int skip, int take, CancellationToken token)
        {
            var rows = preview.Groups.SelectMany(group => group.Books).OrderBy(book => book.Number is null).ThenBy(book => book.Number)
                .Select(book => new SeriesBook(book.Id, book.Title, book.Authors, book.Number)).ToArray();
            return Task.FromResult(new SeriesBookPage(rows.Skip(skip).Take(take).ToArray(), rows.Length));
        }
        public Task<SeriesMergePreview?> PreviewAsync(IReadOnlyCollection<string> names, CancellationToken token) => Task.FromResult<SeriesMergePreview?>(preview);
        public Task<SeriesMergeCompletion> MergeAsync(SeriesMergePreview confirmed, string targetName, CancellationToken token)
        {
            Merges++;
            return Task.FromResult(new SeriesMergeCompletion(new(SeriesMergeStatus.Merged, targetName,
                confirmed.Groups.SelectMany(group => group.Books).Where(book => book.StoredName != targetName).ToArray()), []));
        }
        public Task<SeriesRenamePreview> PreviewAsync(string name, CancellationToken token) => throw new NotSupportedException();
        public Task<SeriesRenamePreview> PreviewRenameAsync(string name, CancellationToken token) => throw new NotSupportedException();
        public Task<SeriesRenameResult> RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
        Task<SeriesRenameCompletion> ISeriesRenameService.RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
    }
}

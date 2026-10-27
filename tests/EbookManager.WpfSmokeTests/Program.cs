using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Linq;
using EbookManager.App.Views;
using EbookManager.Application.Series;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Series;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.WpfSmokeTests;

// Run on an interactive Windows desktop; uses only fictitious data, never Saga's startup or library.
internal static class Program
{
    [STAThread]
    private static int Main()
    {
        var application = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("pack://application:,,,/Saga;component/Resources/Styles/Themes/LightTheme.xaml") });
        var root = FindRepository();
        var splitter = XDocument.Load(Path.Combine(root, "src/EbookManager.App/App.xaml")).Descendants()
            .Single(element => element.Name.LocalName == "Style");
        splitter.SetAttributeValue(XNamespace.Xmlns + "x", "http://schemas.microsoft.com/winfx/2006/xaml");
        application.Resources["VisibleVerticalGridSplitterStyle"] = System.Windows.Markup.XamlReader.Parse(splitter.ToString());
        var exitCode = 1;
        application.Dispatcher.InvokeAsync(async () =>
        {
            var repository = new SeriesRepository();
            using var vm = new SeriesManagementViewModel(repository, repository, key => key);
            var window = new SeriesManagementWindow(vm);
            try
            {
                window.Show();
                window.Activate();
                await IdleAsync();
                await vm.SearchAsync("");
                await IdleAsync();
                var list = (ListBox)window.FindName("SeriesList");
                list.Focus();
                Require(list.IsKeyboardFocusWithin, "Series list must have keyboard focus before selection.");
                SendKey(list, Key.Home);
                await WaitAsync(() => !vm.IsBusy && vm.TotalBooks == 1);
                await IdleAsync();
                Require(list.IsKeyboardFocusWithin, "Keyboard focus was lost after loading a selected series.");
                SendKey(list, Key.Down);
                await WaitAsync(() => !vm.IsBusy && vm.Name == "Second");
                await IdleAsync();
                Require(list.IsKeyboardFocusWithin, "Keyboard focus was lost after selecting the next series.");
                Console.WriteLine("PASS: keyboard focus remains in the series list after consecutive loads.");
                repository.FailLoad = true;
                list.SelectedIndex = 0;
                await WaitAsync(() => !vm.IsBusy && vm.StatusMessage == "SeriesLoadFailed");
                await IdleAsync();
                Require(list.IsKeyboardFocusWithin, "Keyboard focus was lost after a failed series load.");
                repository.FailLoad = false;
                list.SelectedIndex = 1;
                await WaitAsync(() => !vm.IsBusy && vm.TotalBooks == 1);
                Console.WriteLine("PASS: keyboard focus also returns after a load failure.");

                var search = (TextBox)window.FindName("SearchBox");
                list.Focus();
                await vm.SelectAsync(new("First", 1));
                search.Focus();
                await IdleAsync();
                Require(search.IsKeyboardFocusWithin, "Delayed restoration stole a newer choice of keyboard focus.");
                Console.WriteLine("PASS: delayed restoration respects a newer choice of focus.");
                search.Focus();
                foreach (var character in "First")
                {
                    search.AppendText(character.ToString());
                    await Task.Delay(250);
                    await WaitAsync(() => !vm.IsSearching);
                    await IdleAsync();
                    Require(search.IsKeyboardFocusWithin, "Typing lost search-box focus after results refreshed.");
                }
                Require(search.Text == "First", "Search text changed while typing.");
                Console.WriteLine("PASS: search text keeps focus across successive result refreshes.");

                var rename = (Button)window.FindName("RenameSeriesButton");
                foreach (var confirm in new[] { false, true })
                {
                    rename.Focus();
                    var closeDialog = CompleteDialogAsync(application, confirm);
                    rename.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    await closeDialog;
                    await WaitAsync(() => !vm.IsBusy && (!confirm || vm.Name == "New name"));
                    await IdleAsync();
                    Require(rename.IsKeyboardFocusWithin, "Rename button did not regain focus after the dialog or save.");
                }
                Console.WriteLine("PASS: rename dialog retains focus; cancelling and saving return focus to its button.");
                await BookshelfSelectionSmoke.RunAsync();
                exitCode = 0;
            }
            catch (Exception exception) { Console.Error.WriteLine("FAIL: " + exception.Message); }
            finally { window.Close(); application.Shutdown(); }
        });
        application.Run();
        return exitCode;
    }

    private static Task IdleAsync() => Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;
    private static void SendKey(UIElement target, Key key)
    {
        var source = PresentationSource.FromVisual(target) ?? throw new InvalidOperationException("The test window is not displayed.");
        (Keyboard.FocusedElement as UIElement ?? target).RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key)
        { RoutedEvent = Keyboard.KeyDownEvent });
    }
    private static async Task CompleteDialogAsync(System.Windows.Application application, bool confirm)
    {
        await WaitAsync(() => application.Windows.OfType<SeriesRenameWindow>().Any(window => window.IsLoaded));
        var dialog = application.Windows.OfType<SeriesRenameWindow>().Single();
        try
        {
            await Task.Delay(50);
            var name = (TextBox)dialog.FindName("NewNameBox");
            Require(dialog.IsActive && name.IsKeyboardFocusWithin, "Busy-state restoration stole focus from the rename dialog.");
            if (confirm) name.Text = "New name";
            dialog.DialogResult = confirm;
        }
        finally { if (dialog.IsVisible) dialog.Close(); }
    }
    private static async Task WaitAsync(Func<bool> completed)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!completed())
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("WPF operation did not complete.");
            await Task.Delay(10);
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static string FindRepository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EbookManager.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Run this smoke test from its repository build.");
    }

    private sealed class SeriesRepository : ISeriesRepository, ISeriesRenameService
    {
        public bool FailLoad { get; set; }
        private static readonly SeriesSourceBook Book = new(Guid.NewGuid(), "First", "Book");
        public Task<SeriesPage> SearchAsync(string query, int skip, int take, CancellationToken token) => Task.FromResult(new SeriesPage([new("First", 1), new("Second", 1)], 2));
        public async Task<SeriesBookPage> ListBooksAsync(string name, int skip, int take, CancellationToken token)
        {
            await Task.Delay(30, token);
            if (FailLoad) throw new IOException("Simulated read failure");
            return new([new(Book.Id, "Book", "Author", 1)], 1);
        }
        public Task<SeriesRenamePreview> PreviewRenameAsync(string name, CancellationToken token) => PreviewAsync(name, token);
        public async Task<SeriesRenamePreview> PreviewAsync(string name, CancellationToken token)
        { await Task.Delay(30, token); return new(name, [Book]); }
        public Task<SeriesRenameResult> RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token) => Task.FromResult(new SeriesRenameResult(SeriesRenameStatus.Renamed, name, preview.Books));
        async Task<SeriesRenameCompletion> ISeriesRenameService.RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token)
        { await Task.Delay(30, token); return new(await RenameAsync(preview, name, token), []); }
    }
}

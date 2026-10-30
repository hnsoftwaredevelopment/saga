using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EbookManager.App.Views;
using EbookManager.Application.Authors;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.WpfSmokeTests;

internal static class AuthorDuplicateDiscoverySmoke
{
    public static async Task RunAsync()
    {
        var state = new AuthorMergeSmoke.Authors();
        using var manager = new AuthorManagementViewModel(state, state, Text, mergeService: state);
        using var vm = new AuthorDuplicatesViewModel(manager.CreateDuplicateIndex(), Text);
        var window = new AuthorDuplicatesWindow(vm, manager);
        try
        {
            window.Show(); window.Activate();
            await WaitAsync(() => !vm.IsWorking && vm.Candidates.Count == 1); await IdleAsync();
            var search = (TextBox)window.FindName("SearchBox");
            var grid = (DataGrid)window.FindName("CandidatesGrid");
            var compare = (Button)window.FindName("CompareButton");
            Require(!compare.IsEnabled && state.Merges == 0, "Duplicate detection must not select or merge authors automatically.");
            search.Focus(); search.Text = "absent";
            await WaitAsync(() => !vm.IsWorking && vm.Candidates.Count == 0); await IdleAsync();
            Require(search.IsKeyboardFocusWithin, "Filtering author suggestions stole search focus.");
            search.Text = "Nowee";
            await WaitAsync(() => !vm.IsWorking && vm.Candidates.Count == 1); await IdleAsync();
            grid.SelectedIndex = 0; await IdleAsync();
            Require(compare.IsEnabled && vm.Selected!.Reason == Text("AuthorDuplicatesInitials"), "Selected suggestion does not show an understandable reason or enable comparison.");
            if (Environment.GetEnvironmentVariable("SAGA_WPF_CAPTURE_AUTHOR_COMPARISON") is { Length: > 0 } directory)
                foreach (var theme in new[] { "Light", "Dark" })
                {
                    System.Windows.Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
                    { Source = new Uri($"pack://application:,,,/Saga;component/Resources/Styles/Themes/{theme}Theme.xaml") };
                    window.UpdateLayout(); await IdleAsync();
                    var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(window);
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var stream = File.Create(Path.Combine(directory, $"author-duplicates-{theme}.png")); encoder.Save(stream);
                }
            foreach (var confirm in new[] { false, true })
            {
                compare.Focus();
                var dialog = CompleteDialogAsync(confirm);
                compare.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await dialog; await WaitAsync(() => !vm.IsWorking); await IdleAsync();
                if (!confirm) Require(state.Merges == 0 && vm.Selected is not null && compare.IsKeyboardFocusWithin, "Cancel changed authors or lost the suggestion/caller focus.");
            }
            Require(window.HasMerged && state.Merges == 1 && vm.Candidates.Count == 0 && !compare.IsEnabled,
                "Merged author pair was not removed from refreshed suggestions.");
            Require(manager.Name == "Johannes Nowee" && manager.TotalBooks == 2 && manager.ChangedBookIds.Count == 2 && vm.LastActionMessage.Length > 0,
                "Discovery merge did not retain the manager result and metadata reconciliation IDs.");
            Console.WriteLine("PASS: author duplicate discovery filters readonly suggestions, preserves cancel/focus and refreshes after explicit safe comparison/merge.");
        }
        finally
        {
            window.Close();
            System.Windows.Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
            { Source = new Uri("pack://application:,,,/Saga;component/Resources/Styles/Themes/LightTheme.xaml") };
        }
    }
    private static string Text(string key) => EbookManager.App.Localization.LocalizedStrings.Current[key];
    private static async Task CompleteDialogAsync(bool confirm)
    {
        await WaitAsync(() => System.Windows.Application.Current.Windows.OfType<AuthorMergeWindow>().Any(window => window.IsLoaded));
        var dialog = System.Windows.Application.Current.Windows.OfType<AuthorMergeWindow>().Single();
        try
        {
            var vm = (AuthorMergeDialogViewModel)dialog.DataContext;
            vm.Target = vm.Authors.Last(); await IdleAsync();
            Require(vm.CanMerge, "Suggested authors cannot use the existing merge dialog.");
            dialog.DialogResult = confirm;
        }
        finally { if (dialog.IsVisible) dialog.Close(); }
    }
    private static Task IdleAsync() => Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;
    private static async Task WaitAsync(Func<bool> complete)
    { var deadline = DateTime.UtcNow.AddSeconds(8); while (!complete()) { if (DateTime.UtcNow > deadline) throw new TimeoutException("Author discovery timed out."); await Task.Delay(10); } }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

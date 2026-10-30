using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EbookManager.App.Views;
using EbookManager.Domain.Authors;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.WpfSmokeTests;

internal static class AuthorComparisonSmoke
{
    public static async Task RunAsync()
    {
        var first = new AuthorProfile(Guid.NewGuid(), "J. Nowee", string.Join(" ", Enumerable.Repeat("Volledige biografie van de eerste auteur.", 30)), "https://first.example", "first.jpg");
        var second = new AuthorProfile(Guid.NewGuid(), "Johannes Nowee", "Andere biografie", "https://second.example", "second.jpg");
        var vm = new AuthorMergeDialogViewModel(new([new(first, [Guid.NewGuid()]), new(second, [Guid.NewGuid()])]), key => EbookManager.App.Localization.LocalizedStrings.Current[key]);
        var encoder = new JpegBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgra32, null, Enumerable.Repeat((byte)180, 16).ToArray(), 8)));
        using var stream = new MemoryStream(); encoder.Save(stream); var jpeg = stream.ToArray();
        var active = 0; var maximum = 0;
        async Task<byte[]?> Read(Guid id, string path) { active++; maximum = Math.Max(maximum, active); try { await Task.Delay(40); return jpeg; } finally { active--; } }
        var window = new AuthorMergeWindow(vm, Read);
        try
        {
            window.Show(); window.Activate(); await IdleAsync();
            await WaitAsync(() => Descendants<Image>(window).Count(image => image.Source is not null) >= 2);
            Require(maximum <= 2, "Visible author source photos were loaded without the concurrency bound.");
            var cards = (ListBox)window.FindName("ComparisonCards");
            Require(cards.Items.Count == 2 && !vm.CanMerge, "Available source cards or required conflict choices are missing.");
            var sourceButtons = Descendants<Button>(cards).Where(button => button.DataContext is AuthorComparisonCard).ToArray();
            Require(sourceButtons.Length == 8, "Both comparison cards must expose four source actions.");
            sourceButtons[0].Focus();
            var visited = new HashSet<Button>();
            for (var step = 0; step < 40; step++)
            {
                if (Keyboard.FocusedElement is Button focused && sourceButtons.Contains(focused)) visited.Add(focused);
                if (ReferenceEquals(Keyboard.FocusedElement, window.FindName("TargetBox"))) break;
                Require(Keyboard.FocusedElement is UIElement current && current.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)),
                    "Forward Tab navigation stopped inside the comparison cards.");
                await IdleAsync();
            }
            Require(visited.Count == sourceButtons.Length, "Tab skipped source actions on a following author card.");
            Require(ReferenceEquals(Keyboard.FocusedElement, window.FindName("TargetBox")), "Tab did not leave the source cards for the result choices.");
            Console.WriteLine("PASS: Tab reaches every source action across both author cards and continues to the result choices.");
            async Task Choose(Guid id, string key)
            {
                var button = Descendants<Button>(cards).Single(button => button.DataContext is AuthorComparisonCard card && card.Profile.Id == id &&
                    Equals(button.Content, EbookManager.App.Localization.LocalizedStrings.Current[key]));
                button.BringIntoView(); await IdleAsync();
                var invoke = (IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke);
                invoke.Invoke(); await IdleAsync();
            }
            await Choose(second.Id, "AuthorComparisonKeepAuthor");
            await Choose(first.Id, "AuthorComparisonUseBiography");
            await Choose(second.Id, "AuthorComparisonUseWebsite");
            await Choose(first.Id, "AuthorComparisonUsePhoto");
            await WaitAsync(() => !vm.PhotoLoading && ((Image)window.FindName("PhotoPreview")).Source is not null);
            Require(vm.CanMerge && vm.Selection == new AuthorMergeSelection(second.Id, first.Id, second.Id, first.Id), "Source field buttons did not produce the visible result choices.");
            Require(vm.BiographyChoice!.Value == first.Biography && vm.ComparisonAuthors[0].Profile.Biography!.Length > 500, "Comparison truncated the available biography.");
            if (Environment.GetEnvironmentVariable("SAGA_WPF_CAPTURE_AUTHOR_COMPARISON") is { Length: > 0 } directory)
                foreach (var theme in new[] { "Light", "Dark" })
                {
                    System.Windows.Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
                    { Source = new Uri($"pack://application:,,,/Saga;component/Resources/Styles/Themes/{theme}Theme.xaml") };
                    window.UpdateLayout(); await IdleAsync(); Capture(window, Path.Combine(directory, $"author-comparison-{theme}.png"));
                }
            Console.WriteLine("PASS: side-by-side author values/photos and real source buttons produce explicit result choices with bounded photo loading.");
        }
        finally
        {
            window.Close();
            System.Windows.Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
            { Source = new Uri("pack://application:,,,/Saga;component/Resources/Styles/Themes/LightTheme.xaml") };
        }
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i); if (child is T item) yield return item;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static Task IdleAsync() => Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;
    private static async Task WaitAsync(Func<bool> complete)
    { var deadline = DateTime.UtcNow.AddSeconds(8); while (!complete()) { if (DateTime.UtcNow > deadline) throw new TimeoutException("Author comparison timed out."); await Task.Delay(10); } }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Capture(Window window, string path)
    {
        var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(window);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var output = File.Create(path); encoder.Save(output);
    }
}

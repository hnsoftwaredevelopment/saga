using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EbookManager.App.Localization;
using EbookManager.App.Views;
using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Infrastructure.Files;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.WpfSmokeTests;

internal static class BulkExportSmoke
{
    public static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "saga-bulk-export-smoke-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var book = new BookExportSource(Guid.NewGuid(), "De wandelaar", ["Voorbeeld Auteur"], []);
        book = book with { Files = [new(Guid.NewGuid(), book.Id, EbookFormat.Epub, "book.epub", "hash", 7, MetadataWriteBackStatus.NotAttempted, null)] };
        var missing = new BookExportSource(Guid.NewGuid(), "Alleen als PDF beschikbaar", ["Andere Auteur"],
            [new(Guid.NewGuid(), Guid.NewGuid(), EbookFormat.Pdf, "missing.pdf", "hash", 7, MetadataWriteBackStatus.NotAttempted, null)]);
        File.WriteAllText(Path.Combine(root, "book.epub"), "content");
        var destination = Path.Combine(root, "output"); Directory.CreateDirectory(destination);
        File.WriteAllText(Path.Combine(destination, "Voorbeeld Auteur - De wandelaar.epub"), "existing");
        var books = new[] { book, missing };
        var pickerCalls = 0;
        using var vm = new BulkExportViewModel(books, new BulkBookExportService(new Repository(books), new BookFileExportService(new ManagedLibraryFileStore(root))), Text);
        var window = new BulkExportWindow(vm, () => Task.FromResult<string?>(++pickerCalls == 1 ? destination : null));
        try
        {
            window.Show(); window.Activate(); await WaitAsync(() => !vm.IsLoading && vm.Formats.Count == 2); await IdleAsync();
            var combo = (ComboBox)window.FindName("FormatBox"); var start = (Button)window.FindName("StartButton");
            var folder = (Button)window.FindName("FolderButton"); var grid = (DataGrid)window.FindName("ExportGrid");
            Require(combo.IsKeyboardFocusWithin && !start.IsEnabled, "Export must focus format and require a destination.");
            Require(grid.Items.Count == 2 && vm.Rows[1].Outcome == Text("BulkExportFormatMissing"), "Preview must show missing formats without removing selected books.");
            folder.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await IdleAsync(); Require(start.IsEnabled, "Choosing destination did not enable export.");
            folder.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await IdleAsync(); Require(vm.Destination == destination, "Cancelling picker cleared destination.");
            window.Width = window.MinWidth; window.Height = window.MinHeight; window.UpdateLayout(); await IdleAsync();
            Require(start.IsVisible && start.TranslatePoint(new Point(), window).Y + start.ActualHeight < window.ActualHeight, "Export button is outside minimum window bounds.");
            start.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await WaitAsync(() => vm.HasFinished); await IdleAsync();
            Require(vm.IsSuccess && !vm.IsError && vm.Rows.Count == 2 && vm.Rows[0].Outcome == Text("BulkExportExported"), "Successful export did not show file results.");
            Require(File.ReadAllText(Path.Combine(destination, "Voorbeeld Auteur - De wandelaar.epub")) == "existing", "Existing destination was changed.");
            Require(File.ReadAllText(Path.Combine(destination, "Voorbeeld Auteur - De wandelaar (1).epub")) == "content", "Copy with unique name was not created.");
            Require(File.ReadAllText(Path.Combine(root, "book.epub")) == "content" && !Directory.GetFiles(destination, "*.tmp").Any(), "Export changed source or left temporary files.");
            Require(((Button)window.FindName("CloseButton")).IsKeyboardFocusWithin && !start.IsEnabled, "Completed export must focus close and prevent accidental repeats.");
            if (Environment.GetEnvironmentVariable("SAGA_WPF_CAPTURE_EXPORT") is { Length: > 0 } capture)
            {
                Directory.CreateDirectory(capture);
                foreach (var theme in new[] { "Light", "Dark" })
                {
                    System.Windows.Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
                    { Source = new Uri($"pack://application:,,,/Saga;component/Resources/Styles/Themes/{theme}Theme.xaml") };
                    window.UpdateLayout(); await IdleAsync();
                    var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(window);
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var stream = File.Create(Path.Combine(capture, "bulk-export-" + theme + ".png")); encoder.Save(stream);
                }
            }
            window.Close();
            await CheckCancellationAsync(book);
            Console.WriteLine("PASS: bulk export previews availability, preserves picker cancellation, exports unique copies and results, handles Escape safely and retains partial results.");
        }
        finally
        {
            window.Close();
            var resolved = Path.GetFullPath(root);
            Require(resolved.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase) && Path.GetFileName(resolved).StartsWith("saga-bulk-export-smoke-"), "Unexpected cleanup target.");
            Directory.Delete(resolved, true);
        }
    }

    private static async Task CheckCancellationAsync(BookExportSource book)
    {
        var held = new HeldExport(book);
        using var vm = new BulkExportViewModel([book], held, Text);
        var window = new BulkExportWindow(vm);
        try
        {
            window.Show(); window.Activate(); await WaitAsync(() => vm.Formats.Count == 1); vm.Destination = "unused";
            var start = (Button)window.FindName("StartButton"); start.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await held.Started.Task; await IdleAsync();
            window.Close(); Require(window.IsVisible && vm.IsCancelling, "Closing during export must cancel and wait for cleanup.");
            held.Finish.TrySetResult(); await WaitAsync(() => vm.HasFinished); await IdleAsync();
            Require(!vm.IsSuccess && !vm.IsError && vm.Rows.Single().Outcome == Text("BulkExportNotProcessed"), "Cancellation must show neutral unprocessed results.");
            window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), 0, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
            Require(!window.IsVisible, "Escape must close a completed export.");
        }
        finally { held.Finish.TrySetResult(); window.Close(); }
    }
    private static string Text(string key) => LocalizedStrings.Current[key];
    private static async Task IdleAsync() => await Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
    private static async Task WaitAsync(Func<bool> condition)
    {
        var end = DateTime.UtcNow.AddSeconds(10);
        while (!condition()) { if (DateTime.UtcNow > end) throw new TimeoutException("Bulk export UI did not settle."); await Task.Delay(20); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class Repository(IReadOnlyList<BookExportSource> books) : IBookExportRepository
    {
        public Task<IReadOnlyList<BookExportSource>> ReadAsync(IReadOnlyCollection<Guid> ids, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<BookExportSource>>(books.Where(book => ids.Contains(book.Id)).ToArray());
    }
    private sealed class HeldExport(BookExportSource book) : IBulkBookExportService
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Finish { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IReadOnlyList<BookExportSource>> PrepareAsync(IReadOnlyList<BookExportSource> selection, IProgress<BulkExportProgress>? progress, CancellationToken token) => Task.FromResult<IReadOnlyList<BookExportSource>>([book]);
        public async Task<BulkExportResult> ExportAsync(IReadOnlyList<BookExportSource> preview, EbookFormat format, string destination, IProgress<BulkExportProgress>? progress, CancellationToken token)
        {
            Started.TrySetResult(); await Finish.Task;
            return new([new(book.Id, book.Title, "Author", token.IsCancellationRequested ? BulkExportStatus.NotProcessed : BulkExportStatus.Exported)]);
        }
    }
}

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

internal static class TagManagementFocusSmoke
{
    public static async Task RunAsync()
    {
        var repository = new TagRepository();
        using var vm = new TagManagementViewModel(repository, repository, key => EbookManager.App.Localization.LocalizedStrings.Current[key]);
        var window = new TagManagementWindow(vm);
        try
        {
            window.Show(); window.Activate(); await IdleAsync();
            await vm.SearchAsync(""); await IdleAsync();
            var list = (ListBox)window.FindName("TagList");
            var search = (TextBox)window.FindName("SearchBox");
            list.Focus();
            SendKey(list, Key.Home); await WaitAsync(() => !vm.IsBusy && vm.TotalBooks == 1); await IdleAsync();
            Require(list.IsKeyboardFocusWithin, "Tag list lost focus after loading its books.");
            SendKey(list, Key.Down); await WaitAsync(() => !vm.IsBusy && vm.Name == "Second"); await IdleAsync();
            Require(list.IsKeyboardFocusWithin, "Tag list lost focus after the next selection.");
            repository.FailLoad = true;
            list.SelectedIndex = 0; await WaitAsync(() => !vm.IsBusy && vm.IsError); await IdleAsync();
            Require(list.IsKeyboardFocusWithin, "Tag list lost focus after a failed book load.");
            repository.FailLoad = false;
            list.SelectedIndex = 1; await WaitAsync(() => !vm.IsBusy && vm.TotalBooks == 1);
            await vm.SelectAsync(repository.Tags[0]); search.Focus(); await IdleAsync();
            Require(search.IsKeyboardFocusWithin, "Delayed tag focus restoration stole a newer text focus.");
            foreach (var letter in "First")
            {
                search.AppendText(letter.ToString()); await Task.Delay(250);
                await WaitAsync(() => !vm.IsSearching); await IdleAsync();
                Require(search.IsKeyboardFocusWithin, "Typing a tag query lost focus.");
            }
            Require(search.Text == "First", "Tag search text was unexpectedly replaced.");
            Console.WriteLine("PASS: tag list/search focus survives consecutive loads, failures and new focus choices.");

            var rename = (Button)window.FindName("RenameTagButton");
            foreach (var confirm in new[] { false, true })
            {
                rename.Focus();
                var complete = CompleteDialogAsync(confirm);
                rename.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await complete;
                await WaitAsync(() => !vm.IsBusy && (!confirm || vm.Name == "Renamed tag")); await IdleAsync();
                Require(rename.IsKeyboardFocusWithin, "Tag rename button did not regain focus.");
            }
            Require(vm.ChangedBookIds.Count == 1 && vm.Tags.Single().Name == "Renamed tag", "Renamed tag was not retained in the filtered index.");
            Console.WriteLine("PASS: tag rename dialog keeps text focus and restores its caller after cancel/save.");

            if (Environment.GetEnvironmentVariable("SAGA_WPF_CAPTURE_TAGS") is { Length: > 0 } directory)
            {
                foreach (var theme in new[] { "Light", "Dark" })
                {
                    System.Windows.Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
                        { Source = new Uri($"pack://application:,,,/Saga;component/Resources/Styles/Themes/{theme}Theme.xaml") };
                    window.Width = 820; window.Height = 520; window.UpdateLayout(); await IdleAsync();
                    Require(rename.IsVisible && rename.ActualHeight > 0, "Tag rename button is unavailable at the minimum window size.");
                    var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(window);
                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                    using var output = System.IO.File.Create(System.IO.Path.Combine(directory, $"tag-management-{theme}.png"));
                    encoder.Save(output);
                }
            }
        }
        finally
        {
            window.Close();
            System.Windows.Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
                { Source = new Uri("pack://application:,,,/Saga;component/Resources/Styles/Themes/LightTheme.xaml") };
        }
    }

    private static async Task CompleteDialogAsync(bool confirm)
    {
        await WaitAsync(() => System.Windows.Application.Current.Windows.OfType<TagRenameWindow>().Any(window => window.IsLoaded));
        var dialog = System.Windows.Application.Current.Windows.OfType<TagRenameWindow>().Single();
        try
        {
            await Task.Delay(50);
            var name = (TextBox)dialog.FindName("NewNameBox");
            Require(dialog.IsActive && name.IsKeyboardFocusWithin, "Busy restoration stole focus from the tag rename dialog.");
            if (confirm) name.Text = "Renamed tag";
            dialog.DialogResult = confirm;
        }
        finally { if (dialog.IsVisible) dialog.Close(); }
    }
    private static Task IdleAsync() => Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;
    private static void SendKey(UIElement target, Key key) => (Keyboard.FocusedElement as UIElement ?? target).RaiseEvent(
        new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(target), Environment.TickCount, key) { RoutedEvent = Keyboard.KeyDownEvent });
    private static async Task WaitAsync(Func<bool> complete)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!complete())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("Tag WPF operation did not complete.");
            await Task.Delay(10);
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private sealed class TagRepository : ITagRepository, ITagRenameService
    {
        public TagSummary[] Tags { get; } = [new(Guid.NewGuid(), "First", 1), new(Guid.NewGuid(), "Second", 1)];
        private readonly TagSourceBook source = new(Guid.NewGuid(), "Book");
        public bool FailLoad { get; set; }
        public Task<TagPage> SearchAsync(string query, int skip, int take, CancellationToken token)
        {
            var result = Tags.Where(tag => tag.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
            return Task.FromResult(new TagPage(result.Skip(skip).Take(take).ToArray(), result.Length));
        }
        public async Task<TagBookPage> ListBooksAsync(Guid id, int skip, int take, CancellationToken token)
        {
            await Task.Delay(30, token);
            if (FailLoad) throw new System.IO.IOException("Simulated tag read failure");
            return new([new(source.Id, source.Title, "Author")], 1);
        }
        public Task<TagRenamePreview> PreviewRenameAsync(Guid id, CancellationToken token) => PreviewAsync(id, token);
        public async Task<TagRenamePreview> PreviewAsync(Guid id, CancellationToken token)
        {
            await Task.Delay(30, token);
            return new(id, Tags.Single(tag => tag.Id == id).Name, [source]);
        }
        public Task<TagRenameResult> RenameAsync(TagRenamePreview preview, string name, CancellationToken token)
        {
            var index = Array.FindIndex(Tags, tag => tag.Id == preview.TagId);
            Tags[index] = Tags[index] with { Name = name };
            return Task.FromResult(new TagRenameResult(TagRenameStatus.Renamed, preview.TagId, name, preview.Books));
        }
        async Task<TagRenameCompletion> ITagRenameService.RenameAsync(TagRenamePreview preview, string name, CancellationToken token) =>
            new(await RenameAsync(preview, name, token), []);
    }
}

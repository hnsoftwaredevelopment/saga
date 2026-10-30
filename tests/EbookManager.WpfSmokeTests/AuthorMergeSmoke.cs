using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EbookManager.App.Views;
using EbookManager.Application.Authors;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Authors;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.WpfSmokeTests;

internal static class AuthorMergeSmoke
{
    public static async Task RunAsync()
    {
        var state = new Authors();
        using var vm = new AuthorManagementViewModel(state, state, key => EbookManager.App.Localization.LocalizedStrings.Current[key], mergeService: state);
        var window = new AuthorManagementWindow(vm);
        try
        {
            window.Show(); window.Activate(); await IdleAsync();
            await WaitAsync(() => !vm.IsSearching && vm.Authors.Count == 2);
            var list = (ListBox)window.FindName("AuthorsList");
            var merge = (Button)window.FindName("MergeAuthorsButton");
            list.Focus(); list.SelectedItems.Add(vm.Authors[0]);
            await WaitAsync(() => !vm.IsBusy && vm.HasProfile); await IdleAsync();
            Require(list.IsKeyboardFocusWithin, "Author selection lost keyboard focus after loading.");
            list.SelectedItems.Add(vm.Authors[1]); await IdleAsync();
            Require(vm.MergeSelectionCount == 2 && merge.IsEnabled && !vm.CanEdit, "Two authors did not enable merge and protect single-profile editing.");
            state.AddUnrelatedAuthor();
            foreach (var confirm in new[] { false, true })
            {
                merge.Focus();
                var complete = CompleteDialogAsync(confirm);
                merge.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await complete; await WaitAsync(() => !vm.IsBusy); await IdleAsync();
                if (!confirm) Require(vm.MergeSelectionCount == 2 && state.Merges == 0 && merge.IsKeyboardFocusWithin, "Cancel changed data or lost the selected authors/caller focus.");
            }
            Require(state.Merges == 1 && vm.Authors.Count == 2 && vm.Books.Count == 2 && vm.TotalBooks == 2 && vm.MergeSelectionCount == 1,
                "Merge did not refresh the surviving author, books, counts and selection.");
            Require(((TextBox)window.FindName("SearchBox")).Text.Length == 0 && vm.TotalAuthors == 2,
                "Closing a successful merge must clear the search and restore unrelated authors.");
            await Task.Delay(350); await IdleAsync();
            Require(list.SelectedItems.Count == 1 && ((AuthorSummary)list.SelectedItem).Id == vm.SelectedAuthorId, "The survivor is not visibly selected.");
            Console.WriteLine("PASS: author multi-selection, explicit merge dialog, cancel and survivor refresh preserve safe scope and focus.");

            state.AddAuthors(205);
            var search = (TextBox)window.FindName("SearchBox");
            search.Text = " "; search.Text = "";
            await WaitAsync(() => !vm.IsSearching && vm.Authors.Count == 200); await IdleAsync();
            list.Focus(); list.SelectedItems.Add(vm.Authors[0]); await WaitAsync(() => !vm.IsBusy); await IdleAsync();
            list.SelectedItems.Add(vm.Authors[1]); await IdleAsync();
            var selected = vm.MergeAuthorIds.ToHashSet();
            var more = (Button)window.FindName("MoreAuthorsButton");
            more.Focus(); more.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await WaitAsync(() => !vm.IsSearching && vm.Authors.Count == 207); await IdleAsync();
            Require(vm.MergeAuthorIds.ToHashSet().SetEquals(selected) && list.SelectedItems.Count == 2, "Loading more authors lost native selection.");
            search.Text = "Paged";
            await WaitAsync(() => !vm.IsSearching && vm.TotalAuthors == 205); await IdleAsync();
            Require(vm.MergeSelectionCount == 0 && list.SelectedItems.Count == 0 && !merge.IsEnabled, "A new search kept a hidden merge selection.");
            Console.WriteLine("PASS: author selection survives paging and new searches clear hidden selections.");
            state.AddAuthorVariant();
            var finish = CompleteDiscoveryAsync();
            ((Button)window.FindName("FindAuthorDuplicatesButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await finish;
            await WaitAsync(() => !vm.IsBusy && !vm.IsSearching && vm.TotalAuthors == 207); await IdleAsync();
            Require(search.Text.Length == 0 && vm.Authors.Any(author => author.Name.StartsWith("Paged", StringComparison.Ordinal)) && vm.Name == "Johannes Nowee",
                "Closing duplicate discovery after merging must clear the parent query and show unrelated authors while retaining the result profile.");
            Console.WriteLine("PASS: closing merged author discovery clears the parent search and restores all authors without losing the survivor profile.");
        }
        finally { window.Close(); }
    }

    private static async Task CompleteDialogAsync(bool confirm)
    {
        await WaitAsync(() => System.Windows.Application.Current.Windows.OfType<AuthorMergeWindow>().Any(window => window.IsLoaded));
        var dialog = System.Windows.Application.Current.Windows.OfType<AuthorMergeWindow>().Single();
        try
        {
            await Task.Delay(80);
            var vm = (AuthorMergeDialogViewModel)dialog.DataContext;
            var target = (ComboBox)dialog.FindName("TargetBox");
            Require(target.IsKeyboardFocusWithin, "Merge dialog lost target keyboard focus.");
            vm.Target = vm.Authors.Single(author => author.Name == "Johannes Nowee"); await IdleAsync();
            Require(vm.CanMerge, "Empty profile fields should not require meaningless additional choices.");
            if (Environment.GetEnvironmentVariable("SAGA_WPF_CAPTURE_AUTHOR_MERGE") is { Length: > 0 } directory)
                foreach (var theme in new[] { "Light", "Dark" })
                {
                    System.Windows.Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
                    { Source = new Uri($"pack://application:,,,/Saga;component/Resources/Styles/Themes/{theme}Theme.xaml") };
                    dialog.Width = 540; dialog.Height = 420; dialog.UpdateLayout(); await IdleAsync();
                    var bitmap = new RenderTargetBitmap((int)dialog.ActualWidth, (int)dialog.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(dialog);
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var stream = File.Create(Path.Combine(directory, $"author-merge-{theme}.png")); encoder.Save(stream);
                }
            dialog.DialogResult = confirm;
        }
        finally
        {
            if (dialog.IsVisible) dialog.Close();
            System.Windows.Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
            { Source = new Uri("pack://application:,,,/Saga;component/Resources/Styles/Themes/LightTheme.xaml") };
        }
    }
    private static async Task CompleteDiscoveryAsync()
    {
        await WaitAsync(() => System.Windows.Application.Current.Windows.OfType<AuthorDuplicatesWindow>().Any(window => window.IsLoaded));
        var window = System.Windows.Application.Current.Windows.OfType<AuthorDuplicatesWindow>().Single();
        var vm = (AuthorDuplicatesViewModel)window.DataContext;
        await WaitAsync(() => !vm.IsWorking && vm.Candidates.Count == 1); await IdleAsync();
        ((DataGrid)window.FindName("CandidatesGrid")).SelectedIndex = 0; await IdleAsync();
        var confirm = CompleteDialogAsync(true);
        ((Button)window.FindName("CompareButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await confirm;
        await WaitAsync(() => !vm.IsWorking && window.HasMerged);
        window.Close();
    }
    private static Task IdleAsync() => Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;
    private static async Task WaitAsync(Func<bool> complete)
    {
        var deadline = DateTime.UtcNow.AddSeconds(8);
        while (!complete()) { if (DateTime.UtcNow > deadline) throw new TimeoutException("Author WPF operation timed out."); await Task.Delay(10); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    internal sealed class Authors : IAuthorProfileRepository, IAuthorMergeService, IAuthorPhotoStore
    {
        private readonly List<AuthorProfile> profiles = [new(Guid.NewGuid(), "J. Nowee", null, null, null), new(Guid.NewGuid(), "Johannes Nowee", null, null, null)];
        private readonly AuthorBook[] books = [new(Guid.NewGuid(), "Book one", null), new(Guid.NewGuid(), "Book two", null)];
        public int Merges { get; private set; }
        public void AddUnrelatedAuthor() => profiles.Add(new(Guid.NewGuid(), "Unrelated", null, null, null));
        public void AddAuthorVariant() => profiles.Add(new(Guid.NewGuid(), "J.Nowee", null, null, null));
        public void AddAuthors(int count) { for (var i = 0; i < count; i++) profiles.Add(new(Guid.NewGuid(), $"Paged {i:D3}", null, null, null)); }
        public Task<AuthorPage> SearchAsync(string query, int skip, int take, CancellationToken token)
        {
            var all = profiles.Where(profile => profile.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).OrderBy(profile => profile.Name).ToArray();
            return Task.FromResult(new AuthorPage(all.Skip(skip).Take(take).Select(profile => new AuthorSummary(profile.Id, profile.Name, Merges > 0 ? 2 : 1)).ToArray(), all.Length));
        }
        public async Task<AuthorProfile?> GetAsync(Guid id, CancellationToken token) { await Task.Delay(30, token); return profiles.SingleOrDefault(profile => profile.Id == id); }
        public Task<IReadOnlyList<AuthorBook>> ListBooksAsync(Guid id, int skip, int take, CancellationToken token) => Task.FromResult<IReadOnlyList<AuthorBook>>(books.Take(Merges > 0 ? 2 : 1).Skip(skip).Take(take).ToArray());
        public Task<AuthorProfileSaveStatus> SaveAsync(AuthorProfile original, AuthorProfile updated, CancellationToken token) => throw new NotSupportedException();
        public Task<AuthorMergePreview?> PreviewAsync(IReadOnlyCollection<Guid> ids, CancellationToken token) => Task.FromResult<AuthorMergePreview?>(
            new(profiles.Where(profile => ids.Contains(profile.Id)).Select((profile, index) => new AuthorRenamePreview(profile, [books[index].Id])).ToArray()));
        public Task<AuthorMergeCompletion> MergeAsync(AuthorMergePreview preview, AuthorMergeSelection selection, CancellationToken token)
        {
            var target = profiles.Single(profile => profile.Id == selection.TargetId);
            profiles.RemoveAll(profile => preview.Authors.Any(author => author.Profile.Id == profile.Id) && profile.Id != target.Id);
            Merges++;
            return Task.FromResult(new AuthorMergeCompletion(new(AuthorMergeStatus.Merged, target, books), []));
        }
        public Task<byte[]?> ReadAsync(Guid id, string path, CancellationToken token) => Task.FromResult<byte[]?>(null);
        public Task<string> SaveJpegAsync(Guid id, byte[] bytes, CancellationToken token) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id, string path, CancellationToken token) => Task.CompletedTask;
    }
}

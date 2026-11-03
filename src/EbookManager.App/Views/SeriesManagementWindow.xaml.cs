using EbookManager.Domain.Series;
using EbookManager.Presentation.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EbookManager.App.Views;

public partial class SeriesManagementWindow : Window
{
    private readonly SeriesManagementViewModel viewModel;
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? searchDelay;
    private bool changingSelection;
    private bool changingBookSelection;
    private UIElement? focusToRestore;
    private int focusVersion;

    public SeriesManagementWindow(SeriesManagementViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Globalization.CultureInfo.CurrentCulture.IetfLanguageTag);
        DataContext = viewModel;
        Loaded += async (_, _) => { SearchBox.Focus(); await viewModel.SearchAsync(""); };
        viewModel.PropertyChanged += ViewModelChanged;
        Closing += (_, e) => { if (viewModel.IsBusy) e.Cancel = true; };
        Closed += (_, _) =>
        {
            lifetime.Cancel();
            searchDelay?.Cancel();
            searchDelay?.Dispose();
            lifetime.Dispose();
            viewModel.PropertyChanged -= ViewModelChanged;
            viewModel.Dispose();
        };
    }

    private void ViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(viewModel.Books) or nameof(viewModel.SelectedNumberBooks))
            Dispatcher.BeginInvoke(RestoreBookSelection);
        if (e.PropertyName == nameof(viewModel.IsBusy))
        {
            var version = ++focusVersion;
            if (viewModel.IsBusy)
            {
                // IsBusy is raised before the bindings disable CanBrowse/CanRename.
                focusToRestore = Keyboard.FocusedElement is UIElement target && Window.GetWindow(target) == this ? target : null;
            }
            else
            {
                var target = focusToRestore;
                var focusAtCompletion = Keyboard.FocusedElement;
                focusToRestore = null;
                Dispatcher.BeginInvoke(() =>
                {
                    if (version == focusVersion && !viewModel.IsBusy && ReferenceEquals(Keyboard.FocusedElement, focusAtCompletion))
                        RestoreFocus(target);
                }, System.Windows.Threading.DispatcherPriority.Input);
            }
            return;
        }
        if (e.PropertyName is nameof(viewModel.Series) or nameof(viewModel.Name) or nameof(viewModel.MergeSeriesNames))
            Dispatcher.BeginInvoke(RestoreSeriesSelection);
    }

    private async void SearchChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded || viewModel.IsBusy) return;
        searchDelay?.Cancel();
        searchDelay?.Dispose();
        var delay = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        searchDelay = delay;
        try
        {
            var query = SearchBox.Text;
            await Task.Delay(200, delay.Token);
            await viewModel.SearchAsync(query);
        }
        catch (OperationCanceledException) { }
    }

    private async void SeriesSelected(object sender, SelectionChangedEventArgs e)
    {
        if (changingSelection || !viewModel.CanBrowse || viewModel.IsSearching) return;
        var selected = SeriesList.SelectedItems.OfType<SeriesSummary>().ToArray();
        changingSelection = true;
        try { viewModel.SetMergeSeries(selected); if (selected.Length == 1) await viewModel.SelectAsync(selected[0]); }
        finally { changingSelection = false; }
    }

    private void RestoreSeriesSelection()
    {
        if (!IsLoaded) return;
        var names = viewModel.MergeSeriesNames.ToHashSet(SeriesNames.Comparer);
        if (names.SetEquals(SeriesList.SelectedItems.OfType<SeriesSummary>().Select(row => row.Name))) return;
        var wasChanging = changingSelection; changingSelection = true;
        try
        {
            SeriesList.SelectedItems.Clear();
            foreach (var row in viewModel.Series.Where(row => names.Contains(row.Name))) SeriesList.SelectedItems.Add(row);
        }
        finally { changingSelection = wasChanging; }
    }
    private async void MergeSeriesClicked(object sender, RoutedEventArgs e)
    {
        searchDelay?.Cancel();
        var preview = await viewModel.PreviewSelectedSeriesAsync();
        if (preview is null) return;
        var dialog = new SeriesMergeDialogViewModel(preview, key => Localization.LocalizedStrings.Current[key]);
        var confirmed = new SeriesMergeWindow(dialog) { Owner = this }.ShowDialog() == true;
        RestoreFocus(sender as UIElement);
        if (confirmed) await viewModel.MergeCandidateAsync(preview, dialog.TargetName);
        RestoreSeriesSelection();
        RestoreFocus(viewModel.CanMergeSelectedSeries ? sender as UIElement : SeriesList);
    }

    private async void RenameClicked(object sender, RoutedEventArgs e)
    {
        searchDelay?.Cancel();
        var preview = await viewModel.PreviewRenameAsync();
        if (preview is null) return;
        var dialog = new SeriesRenameDialogViewModel(preview, key => Localization.LocalizedStrings.Current[key]);
        var confirmed = new SeriesRenameWindow(dialog) { Owner = this }.ShowDialog() == true;
        // A modal dialog can run before the queued busy-state restoration. Restore its caller on return.
        RestoreFocus(sender as UIElement);
        if (confirmed)
            await viewModel.RenameAsync(preview, dialog.NewName);
    }
    private void FindDuplicatesClicked(object sender, RoutedEventArgs e)
    {
        if (!viewModel.CanFindDuplicates) return;
        searchDelay?.Cancel();
        using var duplicates = new SeriesDuplicatesViewModel(viewModel.CreateDuplicateIndex(), key => Localization.LocalizedStrings.Current[key]);
        new SeriesDuplicatesWindow(duplicates, viewModel) { Owner = this }.ShowDialog();
        RestoreFocus(sender as UIElement);
    }
    private async void NumberClicked(object sender, RoutedEventArgs e)
    {
        searchDelay?.Cancel();
        var preview = await viewModel.PreviewNumberAsync();
        if (preview is null) return;
        var dialog = new SeriesNumberDialogViewModel(preview, key => Localization.LocalizedStrings.Current[key]);
        var confirmed = new SeriesNumberWindow(dialog) { Owner = this }.ShowDialog() == true;
        RestoreFocus(sender as UIElement);
        if (!confirmed) return;
        await viewModel.ChangeNumberAsync(preview, dialog.Number);
        if (viewModel.SelectedBook is { } selected) BooksGrid.ScrollIntoView(selected);
        RestoreFocus(viewModel.CanEditNumber ? sender as UIElement : BooksGrid);
    }
    private void NumberBooksSelected(object sender, SelectionChangedEventArgs e)
    {
        if (!changingBookSelection && !viewModel.IsBusy) viewModel.SetNumberSelection(BooksGrid.SelectedItems.OfType<SeriesBook>());
    }
    private void RestoreBookSelection()
    {
        if (!IsLoaded || viewModel.IsBusy) return;
        var ids = viewModel.SelectedNumberBooks.Select(book => book.Id).ToHashSet();
        if (ids.Count == 0 && viewModel.SelectedBook is { } single) ids.Add(single.Id);
        var rows = viewModel.Books.Where(book => ids.Contains(book.Id)).ToArray();
        if (BooksGrid.SelectedItems.OfType<SeriesBook>().Select(book => book.Id).ToHashSet().SetEquals(rows.Select(book => book.Id))) return;
        changingBookSelection = true;
        try { BooksGrid.SelectedItems.Clear(); foreach (var row in rows) BooksGrid.SelectedItems.Add(row); }
        finally { changingBookSelection = false; }
        viewModel.SetNumberSelection(rows);
    }
    private async void NumbersClicked(object sender, RoutedEventArgs e)
    {
        searchDelay?.Cancel();
        var preview = await viewModel.PreviewNumbersAsync();
        if (preview is null) return;
        var dialog = new SeriesNumbersDialogViewModel(preview, key => Localization.LocalizedStrings.Current[key]);
        var confirmed = new SeriesNumbersWindow(dialog) { Owner = this }.ShowDialog() == true;
        RestoreFocus(sender as UIElement);
        if (!confirmed) return;
        await viewModel.AssignNumbersAsync(preview, dialog.CreateAssignments());
        RestoreBookSelection();
        RestoreFocus(viewModel.CanAssignNumbers ? sender as UIElement : BooksGrid);
    }
    private void RestoreFocus(UIElement? target)
    {
        if (IsLoaded && IsActive && target is { IsVisible: true, IsEnabled: true } && Window.GetWindow(target) == this)
            Keyboard.Focus(target);
    }
    private async void MoreSeriesClicked(object sender, RoutedEventArgs e) => await viewModel.SearchAsync(viewModel.Query, append: true);
    private async void MoreBooksClicked(object sender, RoutedEventArgs e) => await viewModel.LoadMoreBooksAsync();
    private void CloseClicked(object sender, RoutedEventArgs e) { if (viewModel.CanBrowse) Close(); }
}

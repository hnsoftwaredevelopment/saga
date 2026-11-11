using EbookManager.App.Localization;
using EbookManager.Domain.Series;
using EbookManager.Presentation.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EbookManager.App.Views;

public partial class SeriesRenameWindow : Window
{
    private readonly SeriesRenameDialogViewModel viewModel;
    private readonly Func<string, int, CancellationToken, Task<SeriesPage>>? search;
    private readonly Func<string, CancellationToken, Task<SeriesRenamePreview>>? preview;
    private readonly ObservableCollection<SeriesSummary> destinations = [];
    private CancellationTokenSource? searchRequest;
    private CancellationTokenSource? previewRequest;
    private bool choosingDestination;
    private int offset;

    public SeriesRenameWindow(SeriesRenameDialogViewModel viewModel, Func<string, int, CancellationToken, Task<SeriesPage>>? search = null,
        Func<string, CancellationToken, Task<SeriesRenamePreview>>? preview = null)
    {
        this.viewModel = viewModel;
        this.search = search;
        this.preview = preview;
        InitializeComponent();
        Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Globalization.CultureInfo.CurrentCulture.IetfLanguageTag);
        if (viewModel.IsSplit)
        {
            Height = 555;
            MinHeight = 475;
            MoveDestinations.ItemsSource = destinations;
        }
        DataContext = viewModel;
        SaveRenameButton.IsDefault = !viewModel.IsSplit;
        Loaded += (_, _) =>
        {
            NewNameBox.Focus();
            if (!viewModel.IsSplit) NewNameBox.SelectAll();
        };
        Closed += (_, _) => { Cancel(ref searchRequest); Cancel(ref previewRequest); };
    }

    private async void MoveSearchChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded && viewModel.IsSplit && !choosingDestination)
        {
            viewModel.NewName = NewNameBox.Text;
            await SearchDestinationsAsync(append: false);
        }
    }

    private async void MoreDestinationsClicked(object sender, RoutedEventArgs e) => await SearchDestinationsAsync(append: true);

    private async Task SearchDestinationsAsync(bool append)
    {
        if (search is null) return;
        Cancel(ref searchRequest);
        if (!append)
        {
            Cancel(ref previewRequest);
            destinations.Clear();
            offset = 0;
            DestinationStatus.Text = "";
            MoreDestinationsButton.Visibility = Visibility.Collapsed;
            UseNewSeriesButton.Visibility = Visibility.Collapsed;
        }
        var query = NewNameBox.Text.Trim();
        if (query.Length == 0 || SeriesNames.Same(query, viewModel.OriginalName)) return;
        MoreDestinationsButton.IsEnabled = false;
        var request = new CancellationTokenSource();
        searchRequest = request;
        var token = request.Token;
        try
        {
            if (!append) await Task.Delay(180, token);
            var result = await search(query, offset, token);
            if (token.IsCancellationRequested) return;
            foreach (var row in result.Series)
                if (!SeriesNames.Same(row.Name, viewModel.OriginalName) && !destinations.Any(item => SeriesNames.Same(item.Name, row.Name)))
                    destinations.Add(row);
            offset += result.Series.Count;
            MoreDestinationsButton.Visibility = offset < result.TotalCount && result.Series.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (append) return;

            var exact = result.Series.FirstOrDefault(row => SeriesNames.Same(row.Name, query));
            if (exact is not null || offset < result.TotalCount)
            {
                DestinationStatus.Text = LocalizedStrings.Current["SeriesMoveChecking"];
                var target = await ReadDestinationAsync(exact?.Name ?? query, token);
                if (token.IsCancellationRequested) return;
                if (target is not null)
                {
                    ShowDestination(target);
                    return;
                }
                if (exact is not null) throw new InvalidOperationException("Selected destination is no longer available.");
            }
            if (destinations.Count > 0)
            {
                if (viewModel.CanCreateSeries) UseNewSeriesButton.Visibility = Visibility.Visible;
                DestinationStatus.Text = viewModel.CanCreateSeries ? "" : LocalizedStrings.Current["SeriesMoveWholeRename"];
                return;
            }
            viewModel.CanUseNewName = viewModel.CanCreateSeries;
            DestinationStatus.Text = LocalizedStrings.Current[viewModel.CanCreateSeries ? "SeriesMoveNew" : "SeriesMoveWholeRename"];
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!token.IsCancellationRequested) DestinationStatus.Text = LocalizedStrings.Current["SeriesLoadFailed"]; }
        finally
        {
            if (ReferenceEquals(searchRequest, request))
            {
                searchRequest = null;
                request.Dispose();
                MoreDestinationsButton.IsEnabled = true;
            }
        }
    }

    private async void DestinationClicked(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source ||
            ItemsControl.ContainerFromElement(MoveDestinations, source) is not ListBoxItem { Content: SeriesSummary target }) return;
        e.Handled = true;
        await ChooseDestinationAsync(target);
    }

    private async void DestinationKeyDown(object sender, KeyEventArgs e)
    {
        if (!viewModel.IsSplit) return;
        if (e.Key == Key.Enter && MoveDestinations.SelectedItem is SeriesSummary target)
        {
            e.Handled = true;
            await ChooseDestinationAsync(target);
        }
        else if (e.Key == Key.Enter && sender == NewNameBox && UseNewSeriesButton.IsVisible)
        {
            e.Handled = true;
            UseNewSeriesClicked(sender, e);
        }
        else if (sender == NewNameBox && (e.Key == Key.Down || e.Key == Key.Up) && destinations.Count > 0)
        {
            var index = MoveDestinations.SelectedIndex;
            var next = index < 0 && e.Key == Key.Up ? destinations.Count - 1 : index + (e.Key == Key.Down ? 1 : -1);
            MoveDestinations.SelectedIndex = Math.Clamp(next, -1, destinations.Count - 1);
            if (MoveDestinations.SelectedItem is { } selected) MoveDestinations.ScrollIntoView(selected);
            e.Handled = true;
        }
    }

    private void UseNewSeriesClicked(object sender, RoutedEventArgs e)
    {
        Cancel(ref searchRequest);
        destinations.Clear();
        MoreDestinationsButton.Visibility = Visibility.Collapsed;
        UseNewSeriesButton.Visibility = Visibility.Collapsed;
        viewModel.CanUseNewName = true;
        DestinationStatus.Text = LocalizedStrings.Current["SeriesMoveNew"];
        NewNameBox.Focus();
    }

    private async Task ChooseDestinationAsync(SeriesSummary target)
    {
        Cancel(ref searchRequest);
        Cancel(ref previewRequest);
        choosingDestination = true;
        try { viewModel.NewName = target.Name; }
        finally { choosingDestination = false; }
        viewModel.CanUseNewName = false;
        viewModel.Destination = null;
        destinations.Clear();
        MoreDestinationsButton.Visibility = Visibility.Collapsed;
        UseNewSeriesButton.Visibility = Visibility.Collapsed;
        NewNameBox.Focus();
        NewNameBox.CaretIndex = NewNameBox.Text.Length;
        var request = new CancellationTokenSource();
        previewRequest = request;
        var token = request.Token;
        DestinationStatus.Text = LocalizedStrings.Current["SeriesMoveChecking"];
        try
        {
            var result = await ReadDestinationAsync(target.Name, token);
            if (token.IsCancellationRequested) return;
            if (result is null) throw new InvalidOperationException("Selected destination is no longer available.");
            ShowDestination(result);
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!token.IsCancellationRequested) DestinationStatus.Text = LocalizedStrings.Current["SeriesLoadFailed"]; }
        finally
        {
            if (ReferenceEquals(previewRequest, request))
            {
                previewRequest = null;
                request.Dispose();
            }
        }
    }

    private async Task<SeriesMoveDestination?> ReadDestinationAsync(string name, CancellationToken token)
    {
        if (preview is null) return null;
        var result = await preview(name, token);
        var canonical = result.Books.Select(book => book.StoredName.Trim()).Order(StringComparer.Ordinal).FirstOrDefault();
        return canonical is null ? null : SeriesMoveDestination.FromBooks(canonical, viewModel.SplitBooks, result.Books);
    }

    private void ShowDestination(SeriesMoveDestination target)
    {
        viewModel.Destination = target;
        DestinationStatus.Text = LocalizedStrings.Current["SeriesMoveExisting"];
    }

    private static void Cancel(ref CancellationTokenSource? request)
    {
        request?.Cancel();
        request?.Dispose();
        request = null;
    }

    private void RenameClicked(object sender, RoutedEventArgs e)
    {
        if (viewModel.CanSave) DialogResult = true;
    }
}

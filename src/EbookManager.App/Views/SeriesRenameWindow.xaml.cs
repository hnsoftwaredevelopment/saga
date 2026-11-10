using EbookManager.App.Localization;
using EbookManager.Domain.Series;
using EbookManager.Presentation.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace EbookManager.App.Views;

public partial class SeriesRenameWindow : Window
{
    private readonly SeriesRenameDialogViewModel viewModel;
    private readonly Func<string, int, CancellationToken, Task<SeriesPage>>? search;
    private readonly Func<string, CancellationToken, Task<SeriesRenamePreview>>? preview;
    private readonly ObservableCollection<SeriesSummary> destinations = [];
    private CancellationTokenSource? searchRequest;
    private CancellationTokenSource? previewRequest;
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
        Loaded += async (_, _) =>
        {
            if (viewModel.IsSplit) { MoveSearchBox.Focus(); await SearchDestinationsAsync(append: false); }
            else { NewNameBox.Focus(); NewNameBox.SelectAll(); }
        };
        Closed += (_, _) => { Cancel(ref searchRequest); Cancel(ref previewRequest); };
    }

    private async void MoveSearchChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded && viewModel.IsSplit) await SearchDestinationsAsync(append: false);
    }

    private async void MoreDestinationsClicked(object sender, RoutedEventArgs e) => await SearchDestinationsAsync(append: true);

    private async Task SearchDestinationsAsync(bool append)
    {
        if (search is null) return;
        Cancel(ref searchRequest);
        if (!append)
        {
            destinations.Clear();
            offset = 0;
            DestinationStatus.Text = "";
            MoreDestinationsButton.Visibility = Visibility.Collapsed;
        }
        MoreDestinationsButton.IsEnabled = false;
        var request = new CancellationTokenSource();
        searchRequest = request;
        var token = request.Token;
        var query = MoveSearchBox.Text;
        try
        {
            if (!append && query.Length > 0) await Task.Delay(180, token);
            var result = await search(query, offset, token);
            if (token.IsCancellationRequested) return;
            foreach (var row in result.Series)
                if (!SeriesNames.Same(row.Name, viewModel.OriginalName) && !destinations.Any(item => SeriesNames.Same(item.Name, row.Name)))
                    destinations.Add(row);
            offset += result.Series.Count;
            MoreDestinationsButton.Visibility = offset < result.TotalCount && result.Series.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
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

    private async void DestinationSelected(object sender, SelectionChangedEventArgs e)
    {
        Cancel(ref previewRequest);
        viewModel.Destination = null;
        DestinationStatus.Text = "";
        if (preview is null || MoveDestinations.SelectedItem is not SeriesSummary target) return;
        var request = new CancellationTokenSource();
        previewRequest = request;
        var token = request.Token;
        DestinationStatus.Text = LocalizedStrings.Current["SeriesMoveChecking"];
        try
        {
            var result = await preview(target.Name, token);
            if (token.IsCancellationRequested) return;
            if (result.Books.Count == 0) throw new InvalidOperationException("Selected destination is no longer available.");
            viewModel.Destination = SeriesMoveDestination.FromBooks(target.Name, viewModel.SplitBooks, result.Books);
            DestinationStatus.Text = "";
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

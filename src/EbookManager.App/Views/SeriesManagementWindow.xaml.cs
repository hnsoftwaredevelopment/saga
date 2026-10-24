using EbookManager.Domain.Series;
using EbookManager.Presentation.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace EbookManager.App.Views;

public partial class SeriesManagementWindow : Window
{
    private readonly SeriesManagementViewModel viewModel;
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? searchDelay;
    private bool changingSelection;

    public SeriesManagementWindow(SeriesManagementViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
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
        if (e.PropertyName is not (nameof(viewModel.Series) or nameof(viewModel.Name))) return;
        Dispatcher.BeginInvoke(() =>
        {
            if (!IsLoaded) return;
            changingSelection = true;
            SeriesList.SelectedItem = viewModel.Series.FirstOrDefault(series => SeriesNames.Same(series.Name, viewModel.Name));
            changingSelection = false;
        });
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
        if (changingSelection || SeriesList.SelectedItem is not SeriesSummary series || !viewModel.CanBrowse) return;
        await viewModel.SelectAsync(series);
    }

    private async void RenameClicked(object sender, RoutedEventArgs e)
    {
        searchDelay?.Cancel();
        var preview = await viewModel.PreviewRenameAsync();
        if (preview is null) return;
        var dialog = new SeriesRenameDialogViewModel(preview, key => Localization.LocalizedStrings.Current[key]);
        if (new SeriesRenameWindow(dialog) { Owner = this }.ShowDialog() == true)
            await viewModel.RenameAsync(preview, dialog.NewName);
    }
    private async void MoreSeriesClicked(object sender, RoutedEventArgs e) => await viewModel.SearchAsync(viewModel.Query, append: true);
    private async void MoreBooksClicked(object sender, RoutedEventArgs e) => await viewModel.LoadMoreBooksAsync();
    private void CloseClicked(object sender, RoutedEventArgs e) { if (viewModel.CanBrowse) Close(); }
}

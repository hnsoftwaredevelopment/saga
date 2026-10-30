using System.Windows;
using System.Windows.Controls;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class AuthorDuplicatesWindow : Window
{
    private readonly AuthorDuplicatesViewModel viewModel;
    private readonly AuthorManagementViewModel manager;
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? searchDelay;
    public bool HasMerged { get; private set; }
    public AuthorDuplicatesWindow(AuthorDuplicatesViewModel viewModel, AuthorManagementViewModel manager)
    {
        this.viewModel = viewModel;
        this.manager = manager;
        InitializeComponent(); DataContext = viewModel;
        Loaded += async (_, _) => { SearchBox.Focus(); await viewModel.ReloadAsync(); };
        Closing += (_, args) => { if (manager.IsBusy) args.Cancel = true; };
        Closed += (_, _) => { lifetime.Cancel(); searchDelay?.Cancel(); searchDelay?.Dispose(); lifetime.Dispose(); viewModel.Dispose(); };
    }
    private async void SearchChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;
        searchDelay?.Cancel(); searchDelay?.Dispose();
        var delay = searchDelay = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        try { var text = SearchBox.Text; await Task.Delay(200, delay.Token); await viewModel.SearchAsync(text); }
        catch (OperationCanceledException) { }
    }
    private async void CompareClicked(object sender, RoutedEventArgs e)
    {
        if (!viewModel.CanCompare || viewModel.Selected is not { } selected) return;
        searchDelay?.Cancel();
        viewModel.IsComparing = true;
        try
        {
            var preview = await manager.PreviewCandidateAsync(selected.Candidate);
            if (preview is null) { CopyResult(); await viewModel.ReloadAsync(SearchBox.Text); return; }
            var dialog = new AuthorMergeDialogViewModel(preview, key => Localization.LocalizedStrings.Current[key]);
            if (new AuthorMergeWindow(dialog, manager.ReadMergePhotoAsync) { Owner = this }.ShowDialog() != true) return;
            var merged = await manager.MergeCandidateAsync(preview, dialog.Selection);
            CopyResult();
            if (merged) { HasMerged = true; await viewModel.ReloadAsync(SearchBox.Text); }
        }
        finally
        {
            viewModel.IsComparing = false;
            if (IsLoaded && SearchBox.Text != viewModel.Query) await viewModel.SearchAsync(SearchBox.Text);
            if (CompareButton.IsEnabled) CompareButton.Focus(); else CandidatesGrid.Focus();
        }
    }
    private void CopyResult()
    {
        viewModel.LastActionMessage = manager.StatusMessage;
        viewModel.LastActionIsError = manager.IsError;
        viewModel.Warnings = manager.RenameWarnings;
    }
    private async void RefreshClicked(object sender, RoutedEventArgs e)
    { searchDelay?.Cancel(); await viewModel.ReloadAsync(SearchBox.Text); }
    private void CloseClicked(object sender, RoutedEventArgs e) => Close();
}

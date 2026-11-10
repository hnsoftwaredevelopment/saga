using EbookManager.Domain.Tags;
using EbookManager.Presentation.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EbookManager.App.Views;

public partial class TagAssignmentWindow : Window
{
    private readonly TagAssignmentDialogViewModel viewModel;
    private readonly Func<string, CancellationToken, Task<TagPage>>? search;
    private CancellationTokenSource? searchDelay;

    public TagAssignmentWindow(TagAssignmentDialogViewModel viewModel, Func<string, CancellationToken, Task<TagPage>>? search = null)
    {
        this.viewModel = viewModel;
        this.search = search;
        InitializeComponent();
        DataContext = viewModel;
        if (viewModel.IsAdd)
        {
            Height = 490;
            MinHeight = 420;
        }
        Loaded += async (_, _) =>
        {
            if (viewModel.IsAdd) { NewTagBox.Focus(); await RefreshSuggestionsAsync(); }
            else if (viewModel.HasTargetName) { NewNameBox.Focus(); NewNameBox.SelectAll(); }
            else ApplyButton.Focus();
        };
        Closed += (_, _) => CancelSearch();
    }

    private async void TagSearchChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded && viewModel.IsAdd) await RefreshSuggestionsAsync();
    }

    private async Task RefreshSuggestionsAsync()
    {
        if (search is null) return;
        CancelSearch();
        var pending = new CancellationTokenSource();
        searchDelay = pending;
        var token = pending.Token;
        var query = NewTagBox.Text.Trim();
        TagSuggestions.ItemsSource = null;
        TagSearchError.Visibility = Visibility.Collapsed;
        try
        {
            await Task.Delay(180, token);
            var result = await search(query, token);
            if (!token.IsCancellationRequested)
                TagSuggestions.ItemsSource = result.Tags.Where(tag => !viewModel.ContainsTag(tag.Name)).ToArray();
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            if (!token.IsCancellationRequested) TagSearchError.Visibility = Visibility.Visible;
        }
    }

    private void CancelSearch()
    {
        searchDelay?.Cancel();
        searchDelay?.Dispose();
        searchDelay = null;
    }

    private void QueueTagClicked(object sender, RoutedEventArgs e) => QueueTag(viewModel.NewName);

    private void TagInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || string.IsNullOrWhiteSpace(viewModel.NewName)) return;
        e.Handled = true;
        if (viewModel.CanQueueTag) QueueTag(viewModel.NewName);
    }

    private void QueueSuggestionClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: TagSummary tag }) QueueTag(tag.Name);
    }

    private void QueueTag(string name)
    {
        var hadInput = NewTagBox.Text.Length > 0;
        viewModel.QueueTag(name);
        if (!hadInput && TagSuggestions.ItemsSource is IEnumerable<TagSummary> shown)
            TagSuggestions.ItemsSource = shown.Where(tag => !viewModel.ContainsTag(tag.Name)).ToArray();
        NewTagBox.Focus();
    }

    private async void UnqueueTagClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: string tag }) return;
        viewModel.UnqueueTag(tag);
        NewTagBox.Focus();
        await RefreshSuggestionsAsync();
    }

    private void ApplyClicked(object sender, RoutedEventArgs e) { if (viewModel.CanApply) DialogResult = true; }
}

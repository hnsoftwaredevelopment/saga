using EbookManager.Domain.Tags;
using EbookManager.Presentation.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EbookManager.App.Views;

public partial class TagAssignmentWindow : Window
{
    private readonly TagAssignmentDialogViewModel viewModel;
    private readonly Func<string, CancellationToken, Task<IReadOnlyList<string>>>? search;
    private CancellationTokenSource? searchDelay;

    public TagAssignmentWindow(TagAssignmentDialogViewModel viewModel, Func<string, CancellationToken, Task<IReadOnlyList<string>>>? search = null)
    {
        this.viewModel = viewModel;
        this.search = search;
        InitializeComponent();
        DataContext = viewModel;
        ApplyButton.IsDefault = !viewModel.IsAdd;
        if (viewModel.IsAdd)
        {
            Height = 490;
            MinHeight = 420;
            DataObject.AddPastingHandler(NewTagBox, PasteTagLines);
        }
        Loaded += (_, _) =>
        {
            if (viewModel.IsAdd) NewTagBox.Focus();
            else if (viewModel.HasTargetName) { NewNameBox.Focus(); NewNameBox.SelectAll(); }
            else ApplyButton.Focus();
        };
        Closed += (_, _) => CancelSearch();
    }

    private async void TagSearchChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded || !viewModel.IsAdd) return;
        TagLineError.Visibility = Visibility.Collapsed;
        await RefreshSuggestionsAsync();
    }

    private async Task RefreshSuggestionsAsync()
    {
        if (search is null) return;
        CancelSearch();
        TagSuggestions.ItemsSource = null;
        TagSearchError.Visibility = Visibility.Collapsed;
        var query = NewTagBox.Text;
        if (string.IsNullOrWhiteSpace(query)) return;
        var pending = new CancellationTokenSource();
        searchDelay = pending;
        var token = pending.Token;
        try
        {
            await Task.Delay(180, token);
            var result = await search(query, token);
            if (!token.IsCancellationRequested)
                TagSuggestions.ItemsSource = result.ExceptBy(viewModel.QueuedTags.Select(TagNames.Normalize), TagNames.Normalize).ToArray();
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            if (!token.IsCancellationRequested) TagSearchError.Visibility = Visibility.Visible;
        }
        finally
        {
            if (ReferenceEquals(searchDelay, pending)) CancelSearch();
        }
    }

    private void CancelSearch()
    {
        searchDelay?.Cancel();
        searchDelay?.Dispose();
        searchDelay = null;
    }

    private void TagInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && (TagSuggestions.HasItems || searchDelay is not null))
        {
            CancelSearch();
            TagSuggestions.ItemsSource = null;
            NewTagBox.Focus();
            e.Handled = true;
        }
        else if (sender == NewTagBox && (e.Key == Key.Down || e.Key == Key.Up) && TagSuggestions.HasItems)
        {
            var current = TagSuggestions.SelectedIndex;
            var next = current < 0 && e.Key == Key.Up ? TagSuggestions.Items.Count - 1 : current + (e.Key == Key.Down ? 1 : -1);
            TagSuggestions.SelectedIndex = Math.Clamp(next, -1, TagSuggestions.Items.Count - 1);
            if (TagSuggestions.SelectedItem is { } selected) TagSuggestions.ScrollIntoView(selected);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            e.Handled = true;
            var name = TagSuggestions.SelectedItem as string ?? NewTagBox.Text;
            if (!string.IsNullOrWhiteSpace(name)) ConfirmTag(name);
        }
    }

    private void ChooseSuggestion(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source ||
            ItemsControl.ContainerFromElement(TagSuggestions, source) is not ListBoxItem { Content: string tag }) return;
        e.Handled = true;
        ConfirmTag(tag);
    }

    private void ConfirmTag(string name)
    {
        if (viewModel.QueueTag(name)) NewTagBox.Focus();
        else TagLineError.Visibility = Visibility.Visible;
    }

    private void PasteTagLines(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetData(DataFormats.UnicodeText) is not string text || !text.Any(character => character is '\r' or '\n')) return;
        e.CancelCommand();
        var combined = NewTagBox.Text.Remove(NewTagBox.SelectionStart, NewTagBox.SelectionLength).Insert(NewTagBox.SelectionStart, text);
        TagLineError.Visibility = viewModel.QueuePastedTags(combined) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UnqueueTagClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: string tag }) return;
        viewModel.UnqueueTag(tag);
        NewTagBox.Focus();
    }

    private void ApplyClicked(object sender, RoutedEventArgs e) { if (viewModel.CanApply) DialogResult = true; }
}

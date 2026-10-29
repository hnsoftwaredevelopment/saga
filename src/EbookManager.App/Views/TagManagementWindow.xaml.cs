using EbookManager.Domain.Tags;
using EbookManager.Presentation.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EbookManager.App.Views;

public partial class TagManagementWindow : Window
{
    private readonly TagManagementViewModel viewModel;
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? searchDelay;
    private bool changingSelection;
    private UIElement? focusToRestore;
    private int focusVersion;

    public TagManagementWindow(TagManagementViewModel viewModel)
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
        if (e.PropertyName is not (nameof(viewModel.Tags) or nameof(viewModel.Name))) return;
        Dispatcher.BeginInvoke(() =>
        {
            if (!IsLoaded) return;
            changingSelection = true;
            TagList.SelectedItem = viewModel.Tags.FirstOrDefault(tag => tag.Id == viewModel.TagId);
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

    private async void TagSelected(object sender, SelectionChangedEventArgs e)
    {
        if (changingSelection || TagList.SelectedItem is not TagSummary tag || !viewModel.CanBrowse) return;
        await viewModel.SelectAsync(tag);
    }

    private async void RenameClicked(object sender, RoutedEventArgs e)
    {
        searchDelay?.Cancel();
        var preview = await viewModel.PreviewRenameAsync();
        if (preview is null) return;
        var dialog = new TagRenameDialogViewModel(preview, key => Localization.LocalizedStrings.Current[key]);
        var confirmed = new TagRenameWindow(dialog) { Owner = this }.ShowDialog() == true;
        // A modal dialog can run before the queued busy-state restoration. Restore its caller on return.
        RestoreFocus(sender as UIElement);
        if (confirmed)
            await viewModel.RenameAsync(preview, dialog.NewName);
    }
    private void RestoreFocus(UIElement? target)
    {
        if (IsLoaded && IsActive && target is { IsVisible: true, IsEnabled: true } && Window.GetWindow(target) == this)
            Keyboard.Focus(target);
    }
    private async void MoreTagClicked(object sender, RoutedEventArgs e) => await viewModel.SearchAsync(viewModel.Query, append: true);
    private async void MoreBooksClicked(object sender, RoutedEventArgs e) => await viewModel.LoadMoreBooksAsync();
    private void CloseClicked(object sender, RoutedEventArgs e) { if (viewModel.CanBrowse) Close(); }
}

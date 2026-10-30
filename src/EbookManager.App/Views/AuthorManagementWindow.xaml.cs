using EbookManager.Application.Authors;
using EbookManager.App.Services;
using EbookManager.Domain.Authors;
using EbookManager.Presentation.ViewModels;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EbookManager.App.Views;

public partial class AuthorManagementWindow : Window
{
    private readonly AuthorManagementViewModel viewModel;
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? searchDelay;
    private bool changingSelection;
    private bool changingSearchText;
    private bool closingConfirmed;
    private UIElement? focusToRestore;
    private int focusVersion;

    public AuthorManagementWindow(AuthorManagementViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += ProfileChanged;
        Loaded += async (_, _) => { SearchBox.Focus(); await viewModel.SearchAsync(""); };
        Closing += WindowClosing;
        Closed += (_, _) =>
        {
            lifetime.Cancel();
            searchDelay?.Cancel();
            searchDelay?.Dispose();
            lifetime.Dispose();
            viewModel.PropertyChanged -= ProfileChanged;
            viewModel.Dispose();
        };
    }

    private static string Text(string key) => Localization.LocalizedStrings.Current[key];

    private void ProfileChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(viewModel.IsBusy) or nameof(viewModel.IsSearching))
        {
            var version = ++focusVersion;
            if (viewModel.IsWorking)
            {
                if (Keyboard.FocusedElement is UIElement target && Window.GetWindow(target) == this) focusToRestore = target;
            }
            else
            {
                var target = focusToRestore;
                var completedFocus = Keyboard.FocusedElement;
                focusToRestore = null;
                Dispatcher.BeginInvoke(() =>
                {
                    if (version == focusVersion && !viewModel.IsWorking && target?.IsEnabled == true && target.IsVisible &&
                        ReferenceEquals(Keyboard.FocusedElement, completedFocus)) target.Focus();
                }, System.Windows.Threading.DispatcherPriority.Input);
            }
        }
        if (e.PropertyName == nameof(viewModel.PhotoBytes))
            Portrait.Source = AuthorPhotoImageProcessor.Preview(viewModel.PhotoBytes);
        if (e.PropertyName is nameof(viewModel.Authors) or nameof(viewModel.MergeSelectionCount))
        {
            Dispatcher.BeginInvoke(() => RestoreAuthorSelection());
        }
    }

    private async void SearchChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded || changingSearchText) return;
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

    private async void AuthorSelected(object sender, SelectionChangedEventArgs e)
    {
        if (changingSelection || !viewModel.CanChooseAuthors) return;
        var desired = AuthorsList.SelectedItems.OfType<AuthorSummary>().ToArray();
        changingSelection = true;
        try
        {
            if (viewModel.IsBusy || !await ResolvePendingChangesAsync())
            {
                RestoreAuthorSelection();
                return;
            }
            viewModel.SetMergeAuthors(desired);
            if (desired.Length == 1)
            {
                await viewModel.SelectAsync(desired[0]);
            }
        }
        finally { changingSelection = false; }
    }

    private void RestoreAuthorSelection()
    {
        if (!IsLoaded) return;
        var ids = viewModel.MergeAuthorIds.ToHashSet();
        if (ids.SetEquals(AuthorsList.SelectedItems.OfType<AuthorSummary>().Select(author => author.Id))) return;
        var wasChanging = changingSelection;
        changingSelection = true;
        try
        {
            AuthorsList.SelectedItems.Clear();
            foreach (var author in viewModel.Authors.Where(author => ids.Contains(author.Id))) AuthorsList.SelectedItems.Add(author);
        }
        finally { changingSelection = wasChanging; }
    }

    private async void MergeClicked(object sender, RoutedEventArgs e)
    {
        searchDelay?.Cancel();
        if (!viewModel.CanMerge || !await ResolvePendingChangesAsync()) return;
        var preview = await viewModel.PreviewMergeAsync();
        if (preview is null) return;
        var edit = new AuthorMergeDialogViewModel(preview, Text);
        if (new AuthorMergeWindow(edit, viewModel.ReadMergePhotoAsync) { Owner = this }.ShowDialog() == true)
            if (await viewModel.MergeAsync(preview, edit.Selection)) ShowSelectedAuthorQuery();
        RestoreAuthorSelection();
        if (MergeAuthorsButton.IsEnabled) MergeAuthorsButton.Focus(); else AuthorsList.Focus();
    }

    private async void FindDuplicatesClicked(object sender, RoutedEventArgs e)
    {
        searchDelay?.Cancel();
        if (!viewModel.CanFindDuplicates || !await ResolvePendingChangesAsync()) return;
        var duplicates = new AuthorDuplicatesViewModel(viewModel.CreateDuplicateIndex(), Text);
        var window = new AuthorDuplicatesWindow(duplicates, viewModel) { Owner = this };
        window.ShowDialog();
        if (window.HasMerged) ShowSelectedAuthorQuery();
        RestoreAuthorSelection();
        FindAuthorDuplicatesButton.Focus();
    }

    private async Task<bool> ResolvePendingChangesAsync()
    {
        if (!viewModel.HasChanges) return true;
        var result = MessageBox.Show(this, Text("AuthorUnsavedMessage"), Text("AuthorManagementTitle"),
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (result == MessageBoxResult.Cancel) return false;
        if (result == MessageBoxResult.Yes) return await viewModel.SaveAsync();
        viewModel.DiscardChanges();
        return true;
    }

    private async void WindowClosing(object? sender, CancelEventArgs e)
    {
        if (closingConfirmed) return;
        if (viewModel.IsBusy) { e.Cancel = true; return; }
        if (!viewModel.HasChanges) return;
        e.Cancel = true;
        if (await ResolvePendingChangesAsync()) { closingConfirmed = true; Close(); }
    }

    private async void SaveClicked(object sender, RoutedEventArgs e) => await viewModel.SaveAsync();
    private async void RenameClicked(object sender, RoutedEventArgs e)
    {
        if (!viewModel.CanRename || !await ResolvePendingChangesAsync()) return;
        var preview = await viewModel.PreviewRenameAsync();
        if (preview is null) return;
        var edit = new AuthorRenameDialogViewModel(preview, Text);
        var dialog = new AuthorRenameWindow(edit) { Owner = this };
        if (dialog.ShowDialog() == true && await viewModel.RenameAsync(preview, edit.NewName))
            ShowSelectedAuthorQuery();
    }
    private void ShowSelectedAuthorQuery()
    {
        searchDelay?.Cancel();
        changingSearchText = true;
        try { SearchBox.Text = viewModel.Name; }
        finally { changingSearchText = false; }
    }
    private void DiscardClicked(object sender, RoutedEventArgs e) => viewModel.DiscardChanges();
    private void CloseClicked(object sender, RoutedEventArgs e) => Close();
    private async void MoreAuthorsClicked(object sender, RoutedEventArgs e) => await viewModel.SearchAsync(SearchBox.Text, append: true);
    private async void MoreBooksClicked(object sender, RoutedEventArgs e) => await viewModel.LoadMoreBooksAsync();
    private void RemovePhotoClicked(object sender, RoutedEventArgs e) => viewModel.RemovePhoto();

    private async void ChoosePhotoClicked(object sender, RoutedEventArgs e)
    {
        if (!viewModel.CanEdit) return;
        var dialog = new OpenFileDialog { Filter = Text("AuthorPhotoFilter"), CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        viewModel.IsBusy = true;
        try
        {
            var jpeg = await AuthorPhotoImageProcessor.LoadAsync(dialog.FileName);
            viewModel.IsBusy = false;
            viewModel.SetPhoto(jpeg);
        }
        catch (Exception) { viewModel.SetStatus("AuthorPhotoInvalid", true); }
        finally { viewModel.IsBusy = false; }
    }

    private void OpenWebsiteClicked(object sender, RoutedEventArgs e)
    {
        var website = viewModel.Website.Trim();
        if (website.Length == 0) return;
        if (!AuthorProfileValidation.IsValidWebsite(website))
        {
            viewModel.SetStatus("AuthorProfileInvalid", true);
            return;
        }
        try { Process.Start(new ProcessStartInfo(website) { UseShellExecute = true }); }
        catch (Exception) { viewModel.SetStatus("AuthorWebsiteOpenFailed", true); }
    }
}

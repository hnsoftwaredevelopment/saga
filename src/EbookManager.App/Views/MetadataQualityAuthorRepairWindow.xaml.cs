using EbookManager.Presentation.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EbookManager.App.Views;

public partial class MetadataQualityAuthorRepairWindow : Window
{
    private bool isApplyingSuggestion;

    public MetadataQualityAuthorRepairWindow(MetadataQualityAuthorRepairViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void AuthorInputLoaded(object sender, RoutedEventArgs e)
    {
        AuthorInput.Focus();
        Keyboard.Focus(AuthorInput);
        AuthorInput.CaretIndex = AuthorInput.Text.Length;
    }

    private void AuthorInputTextChanged(object sender, TextChangedEventArgs e)
    {
        if (isApplyingSuggestion)
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            if (DataContext is MetadataQualityAuthorRepairViewModel viewModel &&
                viewModel.Suggestions.Count > 0 &&
                AuthorInput.IsKeyboardFocused &&
                !string.IsNullOrWhiteSpace(AuthorInput.Text))
            {
                AuthorSuggestionsPopup.IsOpen = true;
            }
            else
            {
                AuthorSuggestionsPopup.IsOpen = false;
            }
        });
    }

    private void AuthorInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down &&
            AuthorSuggestionsPopup.IsOpen &&
            AuthorSuggestions.Items.Count > 0)
        {
            AuthorSuggestions.SelectedIndex = 0;
            AuthorSuggestions.ScrollIntoView(AuthorSuggestions.SelectedItem);
            Keyboard.Focus(AuthorSuggestions);
            e.Handled = true;
        }
    }

    private void AuthorSuggestionsPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && AuthorSuggestions.SelectedItem is string selectedAuthor)
        {
            UseSuggestion(selectedAuthor);
            e.Handled = true;
        }
    }

    private void AuthorSuggestionsMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var item = ItemsControl.ContainerFromElement(
            AuthorSuggestions,
            e.OriginalSource as DependencyObject) as ListBoxItem;
        if (item?.DataContext is string selectedAuthor)
        {
            UseSuggestion(selectedAuthor);
            e.Handled = true;
        }
    }

    private void UseSuggestion(string selectedAuthor)
    {
        if (DataContext is not MetadataQualityAuthorRepairViewModel viewModel)
        {
            return;
        }

        isApplyingSuggestion = true;
        try
        {
            viewModel.UseSuggestion(selectedAuthor);
            AuthorSuggestionsPopup.IsOpen = false;
            AuthorInput.Focus();
            Keyboard.Focus(AuthorInput);
            AuthorInput.CaretIndex = AuthorInput.Text.Length;
        }
        finally
        {
            isApplyingSuggestion = false;
        }
    }

    private void SaveClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MetadataQualityAuthorRepairViewModel { CanSave: true })
        {
            DialogResult = true;
        }
    }
}

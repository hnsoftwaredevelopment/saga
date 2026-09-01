using EbookManager.Presentation.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EbookManager.App.Views;

public partial class MetadataQualityAuthorRepairWindow : Window
{
    private TextBox? editableTextBox;

    public MetadataQualityAuthorRepairWindow(MetadataQualityAuthorRepairViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void AuthorInputLoaded(object sender, RoutedEventArgs e)
    {
        editableTextBox = AuthorInput.Template.FindName("PART_EditableTextBox", AuthorInput) as TextBox;
        if (editableTextBox is not null)
        {
            editableTextBox.TextChanged += AuthorTextChanged;
        }

        AuthorInput.Focus();
        Keyboard.Focus(editableTextBox is not null ? editableTextBox : AuthorInput);
    }

    private void AuthorTextChanged(object sender, TextChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (DataContext is MetadataQualityAuthorRepairViewModel viewModel &&
                viewModel.Suggestions.Count > 0 &&
                AuthorInput.IsKeyboardFocusWithin)
            {
                AuthorInput.IsDropDownOpen = true;
            }
        });
    }

    private void AuthorInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter &&
            AuthorInput.IsDropDownOpen &&
            AuthorInput.SelectedItem is string selectedAuthor &&
            DataContext is MetadataQualityAuthorRepairViewModel viewModel)
        {
            viewModel.UseSuggestion(selectedAuthor);
            AuthorInput.IsDropDownOpen = false;
            editableTextBox?.CaretIndex = editableTextBox.Text.Length;
            e.Handled = true;
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

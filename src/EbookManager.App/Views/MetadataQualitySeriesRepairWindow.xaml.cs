using EbookManager.Presentation.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EbookManager.App.Views;

public partial class MetadataQualitySeriesRepairWindow : Window
{
    private TextBox? editableTextBox;

    public MetadataQualitySeriesRepairWindow(MetadataQualitySeriesRepairViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void SeriesInputLoaded(object sender, RoutedEventArgs e)
    {
        editableTextBox = SeriesInput.Template.FindName("PART_EditableTextBox", SeriesInput) as TextBox;
        if (editableTextBox is not null)
        {
            editableTextBox.TextChanged += SeriesTextChanged;
        }

        SeriesInput.Focus();
        Keyboard.Focus(editableTextBox is not null ? editableTextBox : SeriesInput);
    }

    private void SeriesTextChanged(object sender, TextChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (DataContext is MetadataQualitySeriesRepairViewModel viewModel &&
                viewModel.Suggestions.Count > 0 &&
                SeriesInput.IsKeyboardFocusWithin)
            {
                SeriesInput.IsDropDownOpen = true;
            }
        });
    }

    private void SeriesInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter &&
            SeriesInput.IsDropDownOpen &&
            SeriesInput.SelectedItem is string selectedSeries &&
            DataContext is MetadataQualitySeriesRepairViewModel viewModel)
        {
            viewModel.UseSuggestion(selectedSeries);
            SeriesInput.IsDropDownOpen = false;
            editableTextBox?.CaretIndex = editableTextBox.Text.Length;
            e.Handled = true;
        }
    }

    private void SaveClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MetadataQualitySeriesRepairViewModel { CanSave: true })
        {
            DialogResult = true;
        }
    }
}

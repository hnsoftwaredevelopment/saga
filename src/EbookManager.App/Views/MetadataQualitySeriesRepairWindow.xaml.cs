using EbookManager.Presentation.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EbookManager.App.Views;

public partial class MetadataQualitySeriesRepairWindow : Window
{
    private bool isApplyingSuggestion;

    public MetadataQualitySeriesRepairWindow(MetadataQualitySeriesRepairViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void SeriesInputLoaded(object sender, RoutedEventArgs e)
    {
        SeriesInput.Focus();
        Keyboard.Focus(SeriesInput);
        SeriesInput.CaretIndex = SeriesInput.Text.Length;
    }

    private void SeriesInputTextChanged(object sender, TextChangedEventArgs e)
    {
        if (isApplyingSuggestion)
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            if (DataContext is MetadataQualitySeriesRepairViewModel viewModel &&
                viewModel.Suggestions.Count > 0 &&
                SeriesInput.IsKeyboardFocused &&
                !string.IsNullOrWhiteSpace(SeriesInput.Text))
            {
                SeriesSuggestionsPopup.IsOpen = true;
            }
            else
            {
                SeriesSuggestionsPopup.IsOpen = false;
            }
        });
    }

    private void SeriesInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down &&
            SeriesSuggestionsPopup.IsOpen &&
            SeriesSuggestions.Items.Count > 0)
        {
            SeriesSuggestions.SelectedIndex = 0;
            SeriesSuggestions.ScrollIntoView(SeriesSuggestions.SelectedItem);
            Keyboard.Focus(SeriesSuggestions);
            e.Handled = true;
        }
    }

    private void SeriesSuggestionsPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && SeriesSuggestions.SelectedItem is string selectedSeries)
        {
            UseSuggestion(selectedSeries);
            e.Handled = true;
        }
    }

    private void SeriesSuggestionsMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var item = ItemsControl.ContainerFromElement(
            SeriesSuggestions,
            e.OriginalSource as DependencyObject) as ListBoxItem;
        if (item?.DataContext is string selectedSeries)
        {
            UseSuggestion(selectedSeries);
            e.Handled = true;
        }
    }

    private void UseSuggestion(string selectedSeries)
    {
        if (DataContext is not MetadataQualitySeriesRepairViewModel viewModel)
        {
            return;
        }

        isApplyingSuggestion = true;
        try
        {
            viewModel.UseSuggestion(selectedSeries);
            SeriesSuggestionsPopup.IsOpen = false;
            SeriesInput.Focus();
            Keyboard.Focus(SeriesInput);
            SeriesInput.CaretIndex = SeriesInput.Text.Length;
        }
        finally
        {
            isApplyingSuggestion = false;
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

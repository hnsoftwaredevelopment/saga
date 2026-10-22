using System.Windows;
using System.Windows.Input;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class MetadataQualityTagRepairWindow : Window
{
    public MetadataQualityTagRepairWindow(MetadataQualityTagRepairViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void TagsInputLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MetadataQualityTagRepairViewModel { IsBulk: true })
        {
            TagPreviews.SelectedIndex = 0;
            TagPreviews.Focus();
            Keyboard.Focus(TagPreviews);
            return;
        }

        TagsInput.Focus();
        Keyboard.Focus(TagsInput);
        TagsInput.CaretIndex = TagsInput.Text.Length;
    }

    private void SaveClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MetadataQualityTagRepairViewModel { CanSave: true })
        {
            DialogResult = true;
        }
    }
}

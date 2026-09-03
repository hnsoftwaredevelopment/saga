using System.Windows;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class MetadataQualityLanguageRepairWindow : Window
{
    public MetadataQualityLanguageRepairWindow(MetadataQualityLanguageRepairViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => LanguageInput.Focus();
    }

    private void SaveClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MetadataQualityLanguageRepairViewModel { CanSave: true })
        {
            DialogResult = true;
        }
    }
}

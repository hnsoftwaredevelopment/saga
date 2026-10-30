using System.Windows;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class SeriesNumberWindow : Window
{
    private readonly SeriesNumberDialogViewModel viewModel;
    public SeriesNumberWindow(SeriesNumberDialogViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent(); DataContext = viewModel;
        Loaded += (_, _) => { NumberBox.Focus(); NumberBox.SelectAll(); };
    }
    private void SaveClicked(object sender, RoutedEventArgs e) { if (viewModel.CanSave) DialogResult = true; }
}

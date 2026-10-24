using EbookManager.Presentation.ViewModels;
using System.Windows;

namespace EbookManager.App.Views;

public partial class SeriesRenameWindow : Window
{
    private readonly SeriesRenameDialogViewModel viewModel;
    public SeriesRenameWindow(SeriesRenameDialogViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => { NewNameBox.Focus(); NewNameBox.SelectAll(); };
    }
    private void RenameClicked(object sender, RoutedEventArgs e)
    {
        if (viewModel.CanSave) DialogResult = true;
    }
}

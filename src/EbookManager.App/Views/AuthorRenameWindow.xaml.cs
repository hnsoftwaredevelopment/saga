using EbookManager.Presentation.ViewModels;
using System.Windows;

namespace EbookManager.App.Views;

public partial class AuthorRenameWindow : Window
{
    private readonly AuthorRenameDialogViewModel viewModel;
    public AuthorRenameWindow(AuthorRenameDialogViewModel viewModel)
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

using EbookManager.Presentation.ViewModels;
using System.Windows;

namespace EbookManager.App.Views;

public partial class TagRenameWindow : Window
{
    private readonly TagRenameDialogViewModel viewModel;
    public TagRenameWindow(TagRenameDialogViewModel viewModel)
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

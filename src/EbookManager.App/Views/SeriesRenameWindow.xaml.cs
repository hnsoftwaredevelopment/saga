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
        if (viewModel.IsSplit)
        {
            Height = 480;
            MinHeight = 420;
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Globalization.CultureInfo.CurrentCulture.IetfLanguageTag);
        }
        DataContext = viewModel;
        Loaded += (_, _) => { NewNameBox.Focus(); NewNameBox.SelectAll(); };
    }
    private void RenameClicked(object sender, RoutedEventArgs e)
    {
        if (viewModel.CanSave) DialogResult = true;
    }
}

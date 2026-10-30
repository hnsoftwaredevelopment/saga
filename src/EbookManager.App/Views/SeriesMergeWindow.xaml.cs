using System.Windows;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class SeriesMergeWindow : Window
{
    private readonly SeriesMergeDialogViewModel viewModel;
    public SeriesMergeWindow(SeriesMergeDialogViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Globalization.CultureInfo.CurrentCulture.IetfLanguageTag);
        DataContext = viewModel;
        Loaded += (_, _) => TargetBox.Focus();
    }
    private void MoreClicked(object sender, RoutedEventArgs e) => viewModel.LoadMore();
    private void MergeClicked(object sender, RoutedEventArgs e) { if (viewModel.CanMerge) DialogResult = true; }
}

using System.Windows;
using System.Windows.Input;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class SeriesNumbersWindow : Window
{
    private readonly SeriesNumbersDialogViewModel viewModel;
    public SeriesNumbersWindow(SeriesNumbersDialogViewModel viewModel)
    {
        this.viewModel = viewModel; InitializeComponent(); DataContext = viewModel;
        Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Globalization.CultureInfo.CurrentCulture.IetfLanguageTag);
        Loaded += (_, _) => { StartBox.Focus(); StartBox.SelectAll(); };
        PreviewKeyDown += (_, e) =>
        {
            if (!ProposalsGrid.IsKeyboardFocusWithin || Keyboard.Modifiers != ModifierKeys.Control || e.Key is not (Key.Up or Key.Down)) return;
            Move(e.Key == Key.Up ? -1 : 1); e.Handled = true;
        };
    }
    private void Move(int offset)
    {
        viewModel.MoveSelected(offset);
        if (viewModel.SelectedRow is { } row) ProposalsGrid.ScrollIntoView(row);
        ProposalsGrid.Focus();
    }
    private void UpClicked(object sender, RoutedEventArgs e) => Move(-1);
    private void DownClicked(object sender, RoutedEventArgs e) => Move(1);
    private void SaveClicked(object sender, RoutedEventArgs e) { if (viewModel.CanSave) DialogResult = true; }
}

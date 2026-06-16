using EbookManager.Presentation.ViewModels;
using System.Windows.Controls;

namespace EbookManager.App.Views;

public partial class DuplicateCandidatesWindow : System.Windows.Window
{
    public DuplicateCandidatesWindow(DuplicateCandidatesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        Close();
    }

    private void DuplicateRowsDoubleClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not DataGrid { SelectedItem: DuplicateCandidateRowViewModel row })
        {
            return;
        }

        var window = new DuplicateCandidateDetailsWindow(row)
        {
            Owner = this
        };
        window.ShowDialog();
    }
}

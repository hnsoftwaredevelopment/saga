using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class ImportHistoryWindow : System.Windows.Window
{
    public ImportHistoryWindow(ImportHistoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        SelectedRun = viewModel.Items.FirstOrDefault();
    }

    public ImportRunSummaryViewModel? SelectedRun { get; set; }

    public Guid? SelectedRunId { get; private set; }

    private void DetailsClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        SelectedRunId = SelectedRun?.RunId;
        DialogResult = SelectedRunId is not null;
    }

    private void CloseClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class MetadataMultiEditWindow : System.Windows.Window
{
    private readonly MetadataMultiEditViewModel viewModel;

    public MetadataMultiEditWindow(MetadataMultiEditViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        DataContext = viewModel;
        viewModel.RequestClose += ViewModelRequestClose;
        Closed += MetadataMultiEditWindowClosed;
    }

    public MetadataMultiEditResult Result => viewModel.CreateResult();

    private void ViewModelRequestClose(object? sender, bool accepted)
    {
        DialogResult = accepted;
    }

    private void MetadataMultiEditWindowClosed(object? sender, EventArgs e)
    {
        viewModel.RequestClose -= ViewModelRequestClose;
    }
}


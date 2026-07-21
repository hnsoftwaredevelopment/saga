using System.Windows;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class Splash : Window
{
    public Splash(
        string subtitle,
        string version,
        string status)
    {
        InitializeComponent();
        SubtitleText.Text = subtitle;
        VersionText.Text = version;
        StatusText.Text = status;
    }

    public void BindLibraryProgress(LibraryViewModel viewModel, string status)
    {
        DataContext = viewModel;
        StatusText.Text = status;
    }
}

using System.Windows;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class Splash : Window
{
    private bool allowClose;

    public Splash(
        string subtitle,
        string version,
        string status)
    {
        InitializeComponent();
        SubtitleText.Text = subtitle;
        VersionText.Text = version;
        StatusText.Text = status;
        Closing += OnClosing;
    }

    public void BindLibraryProgress(LibraryViewModel viewModel, string status)
    {
        DataContext = viewModel;
        StatusText.Text = status;
    }

    public void CloseSplash()
    {
        allowClose = true;
        Close();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = !allowClose;
    }
}

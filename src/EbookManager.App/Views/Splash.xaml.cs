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

    public void ShowStorageMigrationProgress(string status, int processedCount, int totalCount)
    {
        StatusText.Text = status;
        ProgressText.SetCurrentValue(
            System.Windows.Controls.TextBlock.TextProperty,
            totalCount <= 0 ? string.Empty : $"{processedCount} / {totalCount}");
        LibraryProgressBar.SetCurrentValue(
            System.Windows.Controls.ProgressBar.IsIndeterminateProperty,
            totalCount <= 0);
        LibraryProgressBar.SetCurrentValue(
            System.Windows.Controls.ProgressBar.ValueProperty,
            totalCount <= 0 ? 0 : Math.Min(100, processedCount * 100.0 / totalCount));
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

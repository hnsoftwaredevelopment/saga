using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using EbookManager.App.Localization;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class BulkExportWindow : Window
{
    private readonly BulkExportViewModel viewModel;
    private readonly Func<Task<string?>> pickFolder;
    private bool closed;
    public BulkExportWindow(BulkExportViewModel viewModel, Func<Task<string?>>? pickFolder = null)
    {
        this.viewModel = viewModel;
        this.pickFolder = pickFolder ?? (() =>
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog { Title = LocalizedStrings.Current["SelectExportFolder"] };
            return Task.FromResult(dialog.ShowDialog(this) == true ? dialog.FolderName : null);
        });
        InitializeComponent(); DataContext = viewModel;
        Loaded += OnLoaded;
        Closing += OnClosing;
        Closed += (_, _) => { closed = true; viewModel.Dispose(); };
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape) return;
            e.Handled = true;
            if (viewModel.IsRunning) viewModel.Cancel(); else Close();
        };
    }
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await viewModel.LoadAsync();
        if (!closed) FormatBox.Focus();
    }
    private async void StartClicked(object sender, RoutedEventArgs e)
    {
        await viewModel.StartAsync();
        if (!closed && viewModel.HasFinished) CloseButton.Focus();
    }
    private async void FolderClicked(object sender, RoutedEventArgs e)
    {
        if (!viewModel.CanConfigure) return;
        try
        {
            var folder = await pickFolder();
            if (!closed && folder is not null) viewModel.Destination = folder;
        }
        catch (Exception) { if (!closed) viewModel.FolderFailed(); }
    }
    private void CancelClicked(object sender, RoutedEventArgs e) => viewModel.Cancel();
    private void CloseClicked(object sender, RoutedEventArgs e) => Close();
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!viewModel.IsRunning) return;
        e.Cancel = true; viewModel.Cancel();
    }
}

using EbookManager.Presentation.Abstractions;
using EbookManager.Presentation.ViewModels;
using EbookManager.App.Views;
using Microsoft.Win32;

namespace EbookManager.App.Services;

public sealed class UserInteractionService(DeleteConfirmationService deleteConfirmationService) : IUserInteractionService
{
    private readonly DeleteConfirmationService deleteConfirmationService = deleteConfirmationService;

    public Task<IReadOnlyList<string>> PickBookFilesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "E-books|*.epub;*.kepub.epub;*.pdf;*.cbr;*.cbz;*.mobi;*.azw;*.azw3;*.kfx|All files|*.*"
        };

        var result = dialog.ShowDialog() == true
            ? dialog.FileNames.ToArray()
            : [];
        return Task.FromResult<IReadOnlyList<string>>(result);
    }

    public Task<string?> PickScanFolderAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dialog = new OpenFolderDialog
        {
            Title = "Select folder to scan"
        };

        var result = dialog.ShowDialog() == true ? dialog.FolderName : null;
        return Task.FromResult(result);
    }

    public Task<string?> PickLibraryDirectoryAsync(string title, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dialog = new OpenFolderDialog
        {
            Title = title,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        var result = dialog.ShowDialog() == true ? dialog.FolderName : null;
        return Task.FromResult(result);
    }

    public Task<bool> ConfirmDeleteAsync(string title, CancellationToken cancellationToken) =>
        deleteConfirmationService.ConfirmAsync(title, cancellationToken);

    public Task ShowImportResultAsync(ImportResultViewModel result, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var window = new ImportResultWindow(result);
        if (System.Windows.Application.Current?.MainWindow is { } owner)
        {
            window.Owner = owner;
        }

        window.ShowDialog();
        return Task.CompletedTask;
    }

    public Task<Guid?> PickImportRunAsync(ImportHistoryViewModel history, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var window = new ImportHistoryWindow(history);
        if (System.Windows.Application.Current?.MainWindow is { } owner)
        {
            window.Owner = owner;
        }

        var result = window.ShowDialog() == true ? window.SelectedRunId : null;
        return Task.FromResult(result);
    }
}

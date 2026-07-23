using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using EbookManager.App.Localization;
using EbookManager.Domain.Abstractions;
using EbookManager.Presentation.Abstractions;

namespace EbookManager.App.Services;

public sealed class BookFileInteractionService(ILibraryFileStore fileStore) : IBookFileInteractionService
{
    private readonly ILibraryFileStore fileStore = fileStore;

    public Task<bool> OpenFileAsync(string relativePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var absolutePath = fileStore.GetAbsolutePath(relativePath);
        if (!File.Exists(absolutePath))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(TryStartProcess(new ProcessStartInfo
        {
            FileName = absolutePath,
            UseShellExecute = true
        }));
    }

    public Task<bool> OpenContainingFolderAsync(string relativePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var absolutePath = fileStore.GetAbsolutePath(relativePath);
        var fileExists = File.Exists(absolutePath);
        var folderPath = Path.GetDirectoryName(absolutePath);
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return Task.FromResult(false);
        }

        var arguments = fileExists
            ? $"/select,\"{absolutePath}\""
            : $"\"{folderPath}\"";

        return Task.FromResult(TryStartProcess(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = arguments,
            UseShellExecute = true
        }));
    }

    public Task<bool> ConfirmRemoveFormatAsync(string formatText, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var message = string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            LocalizedStrings.Current["RemoveFormatConfirmationMessage"],
            formatText);
        var result = System.Windows.MessageBox.Show(
            message,
            LocalizedStrings.Current["RemoveFormatConfirmationTitle"],
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        return Task.FromResult(result == System.Windows.MessageBoxResult.Yes);
    }

    public Task<string?> PickExportFolderAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = LocalizedStrings.Current["SelectExportFolder"],
            InitialDirectory = GetDownloadsFolder()
        };

        return Task.FromResult(dialog.ShowDialog() == true ? dialog.FolderName : null);
    }

    public Task<string> GetDefaultExportFolderAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetDownloadsFolder());
    }

    private static string GetDownloadsFolder()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrWhiteSpace(userProfile)
            ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            : Path.Combine(userProfile, "Downloads");
    }

    private static bool TryStartProcess(ProcessStartInfo startInfo)
    {
        try
        {
            Process.Start(startInfo);
            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}

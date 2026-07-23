using System.Diagnostics;
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

        Process.Start(new ProcessStartInfo
        {
            FileName = absolutePath,
            UseShellExecute = true
        });

        return Task.FromResult(true);
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

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = arguments,
            UseShellExecute = true
        });

        return Task.FromResult(true);
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
}

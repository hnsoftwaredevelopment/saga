using System.Diagnostics;
using System.IO;
using EbookManager.Domain.Abstractions;
using EbookManager.Presentation.Abstractions;

namespace EbookManager.App.Services;

public sealed class BookFileInteractionService(ILibraryFileStore fileStore) : IBookFileInteractionService
{
    private readonly ILibraryFileStore fileStore = fileStore;

    public Task OpenContainingFolderAsync(string relativePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var absolutePath = fileStore.GetAbsolutePath(relativePath);
        var targetPath = File.Exists(absolutePath)
            ? absolutePath
            : Path.GetDirectoryName(absolutePath);
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return Task.CompletedTask;
        }

        var arguments = File.Exists(absolutePath)
            ? $"/select,\"{absolutePath}\""
            : $"\"{targetPath}\"";

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = arguments,
            UseShellExecute = true
        });

        return Task.CompletedTask;
    }
}

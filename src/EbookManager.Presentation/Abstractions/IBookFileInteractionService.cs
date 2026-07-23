namespace EbookManager.Presentation.Abstractions;

public interface IBookFileInteractionService
{
    Task<bool> OpenFileAsync(string relativePath, CancellationToken cancellationToken);
    Task<bool> OpenContainingFolderAsync(string relativePath, CancellationToken cancellationToken);
    Task<bool> ConfirmRemoveFormatAsync(string formatText, CancellationToken cancellationToken);
    Task<string?> PickExportFolderAsync(CancellationToken cancellationToken);
    Task<string> GetDefaultExportFolderAsync(CancellationToken cancellationToken);
}

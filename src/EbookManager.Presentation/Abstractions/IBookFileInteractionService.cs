namespace EbookManager.Presentation.Abstractions;

public interface IBookFileInteractionService
{
    Task OpenContainingFolderAsync(string relativePath, CancellationToken cancellationToken);
    Task<string?> PickExportFolderAsync(CancellationToken cancellationToken);
    Task<string> GetDefaultExportFolderAsync(CancellationToken cancellationToken);
}

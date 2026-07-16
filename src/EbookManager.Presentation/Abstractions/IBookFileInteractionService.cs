namespace EbookManager.Presentation.Abstractions;

public interface IBookFileInteractionService
{
    Task OpenContainingFolderAsync(string relativePath, CancellationToken cancellationToken);
}

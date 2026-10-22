using EbookManager.Domain.Books;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class LibraryViewModel
{
    public async Task ReconcileExternalMetadataChangesAsync(IReadOnlyCollection<Guid> bookIds, CancellationToken cancellationToken = default)
    {
        if (bookIds.Count == 0) return;
        var libraryPath = CurrentLibraryPath;
        var ids = bookIds.Distinct().ToArray();
        var persisted = await Task.Run(async () =>
        {
            var updated = new List<Book>();
            foreach (var id in ids)
            {
                var book = await bookRepository.GetAsync(id, cancellationToken);
                if (book is not null) updated.Add(book);
            }
            return updated;
        }, cancellationToken);
        if (!string.Equals(libraryPath, CurrentLibraryPath, StringComparison.Ordinal)) return;
        ApplyPersistedMetadataChanges(persisted);
    }
}

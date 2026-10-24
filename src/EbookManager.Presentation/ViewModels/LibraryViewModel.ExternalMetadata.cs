using EbookManager.Domain.Books;
using EbookManager.Domain.Abstractions;

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
            if (bookRepository is IBookMetadataSnapshotRepository snapshots)
                return await snapshots.ListMetadataAsync(ids, cancellationToken);
            var updated = new List<Book>();
            foreach (var id in ids)
            {
                var book = await bookRepository.GetAsync(id, cancellationToken);
                if (book is not null) updated.Add(book);
            }
            return updated;
        }, cancellationToken);
        if (!string.Equals(libraryPath, CurrentLibraryPath, StringComparison.Ordinal)) return;
        // This route changes metadata, not covers. Lightweight snapshots omit existing blobs.
        var existing = books.ToDictionary(book => book.Id);
        var updatedWithCovers = persisted.Select(book =>
        {
            var cover = existing.GetValueOrDefault(book.Id)?.Metadata.CoverBytes;
            if (Details.BookId == book.Id) cover ??= Details.CoverBytes;
            if (book.Metadata.CoverBytes is not null || cover is null) return book;
            var metadata = book.Metadata;
            return book with { Metadata = new BookMetadata(metadata.Title, metadata.Authors,
                metadata.Description, metadata.Language, metadata.Publisher, metadata.PublicationDate,
                metadata.Tags, metadata.Series, metadata.SeriesNumber, metadata.Isbn, cover) };
        }).ToArray();
        ApplyPersistedMetadataChanges(updatedWithCovers);
    }
}

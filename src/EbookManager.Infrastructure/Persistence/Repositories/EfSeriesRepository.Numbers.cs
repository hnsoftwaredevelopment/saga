using EbookManager.Domain.Series;
using EbookManager.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Infrastructure.Persistence.Repositories;

public sealed partial class EfSeriesRepository
{
    public async Task<SeriesNumbersPreview?> PreviewNumbersAsync(string name, IReadOnlyList<Guid> selectedIds, CancellationToken token)
    {
        await using var context = factory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        return await NumbersSnapshotAsync(context, name, selectedIds, token);
    }

    public async Task<SeriesNumbersResult> ChangeNumbersAsync(SeriesNumbersPreview preview, IReadOnlyList<SeriesNumberAssignment> assignments, CancellationToken token)
    {
        var ids = preview.Books.Select(row => row.Book.Id).ToArray();
        var selectedIds = ids.ToHashSet();
        if (assignments.Count != ids.Length || assignments.Select(row => row.Id).Distinct().Count() != ids.Length || assignments.Any(row => !selectedIds.Contains(row.Id)))
            return new(SeriesNumberStatus.Conflict, [], 0);
        await using var context = factory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        var current = await NumbersSnapshotAsync(context, preview.Name, ids, token);
        if (current is null || !current.Books.SequenceEqual(preview.Books)) return new(SeriesNumberStatus.Conflict, [], 0);
        var byId = assignments.ToDictionary(row => row.Id, row => row.Number);
        var numbers = assignments.Select(row => row.Number).ToHashSet();
        var matches = current.OtherNumbers.Count(numbers.Contains);
        var changed = current.Books.Where(row => row.Book.Number != byId[row.Book.Id]).ToArray();
        if (changed.Length == 0) return new(SeriesNumberStatus.Unchanged, [], matches);
        var now = DateTimeOffset.UtcNow;
        foreach (var row in changed)
        {
            // Attach only the two writable properties; never materialize or replace covers/other metadata.
            var entity = new BookEntity { Id = row.Book.Id, SeriesNumber = byId[row.Book.Id], UpdatedUtc = now };
            context.Attach(entity);
            context.Entry(entity).Property(book => book.SeriesNumber).IsModified = true;
            context.Entry(entity).Property(book => book.UpdatedUtc).IsModified = true;
        }
        await context.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return new(SeriesNumberStatus.Changed, changed.Select(row => new SeriesSourceBook(row.Book.Id, row.StoredName, row.Book.Title)).ToArray(), matches);
    }

    private static async Task<SeriesNumbersPreview?> NumbersSnapshotAsync(LibraryDbContext context, string name, IReadOnlyList<Guid> ids, CancellationToken token)
    {
        if (ids.Count < 2 || ids.Distinct().Count() != ids.Count || string.IsNullOrWhiteSpace(name)) return null;
        var selected = new Dictionary<Guid, SeriesNumberSource>();
        foreach (var batch in ids.Chunk(500))
        {
            var rows = await context.Books.AsNoTracking().Where(book => batch.Contains(book.Id))
                .Select(book => new { book.Id, book.Title, book.Series, book.SeriesNumber }).ToArrayAsync(token);
            var authors = (await context.BookAuthors.AsNoTracking().Where(link => batch.Contains(link.BookId)).OrderBy(link => link.Order)
                .Select(link => new { link.BookId, link.Author.Name }).ToArrayAsync(token)).ToLookup(link => link.BookId, link => link.Name);
            foreach (var row in rows)
            {
                if (!SeriesNames.Same(row.Series, name)) return null;
                selected.Add(row.Id, new(row.Series!, new(row.Id, row.Title, string.Join(", ", authors[row.Id]), row.SeriesNumber)));
            }
        }
        if (selected.Count != ids.Count) return null;
        var selectedIds = ids.ToHashSet(); var otherNumbers = new List<decimal>();
        foreach (var names in MatchingNames(await NamesAsync(context, token), name).Chunk(500))
        {
            var numbers = await context.Books.AsNoTracking().Where(book => names.Contains(book.Series!) && book.SeriesNumber != null)
                .Select(book => new { book.Id, Number = book.SeriesNumber!.Value }).ToArrayAsync(token);
            otherNumbers.AddRange(numbers.Where(row => !selectedIds.Contains(row.Id)).Select(row => row.Number));
        }
        return new(name, ids.Select(id => selected[id]).ToArray(), otherNumbers);
    }
}

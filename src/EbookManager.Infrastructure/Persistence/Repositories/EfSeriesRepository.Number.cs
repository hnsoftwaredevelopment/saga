using EbookManager.Domain.Series;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Infrastructure.Persistence.Repositories;

public sealed partial class EfSeriesRepository
{
    public async Task<SeriesNumberPreview?> PreviewNumberAsync(Guid bookId, CancellationToken token)
    {
        await using var context = factory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        return await NumberSnapshotAsync(context, bookId, token);
    }

    public async Task<SeriesNumberResult> ChangeNumberAsync(SeriesNumberPreview preview, decimal? number, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(preview);
        await using var context = factory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        var current = await NumberSnapshotAsync(context, preview.Book.Id, token);
        if (current is null || current.StoredName != preview.StoredName || current.Book != preview.Book)
            return new(SeriesNumberStatus.Conflict, 0);
        var matches = number.HasValue ? current.OtherNumbers.Count(value => value == number.Value) : 0;
        if (current.Book.Number == number) return new(SeriesNumberStatus.Unchanged, matches);
        var changed = await context.Books.Where(book => book.Id == preview.Book.Id).ExecuteUpdateAsync(set => set
            .SetProperty(book => book.SeriesNumber, number).SetProperty(book => book.UpdatedUtc, DateTimeOffset.UtcNow), token);
        if (changed != 1) return new(SeriesNumberStatus.Conflict, 0);
        await transaction.CommitAsync(token);
        return new(SeriesNumberStatus.Changed, matches);
    }

    private static async Task<SeriesNumberPreview?> NumberSnapshotAsync(LibraryDbContext context, Guid id, CancellationToken token)
    {
        var row = await context.Books.AsNoTracking().Where(book => book.Id == id)
            .Select(book => new { book.Id, book.Title, book.Series, book.SeriesNumber }).SingleOrDefaultAsync(token);
        if (row is null || string.IsNullOrWhiteSpace(row.Series)) return null;
        var authors = await context.BookAuthors.AsNoTracking().Where(link => link.BookId == id).OrderBy(link => link.Order)
            .Select(link => link.Author.Name).ToArrayAsync(token);
        var rawNames = MatchingNames(await NamesAsync(context, token), row.Series);
        var numbers = new List<decimal>();
        foreach (var batch in rawNames.Chunk(500))
            numbers.AddRange(await context.Books.AsNoTracking().Where(book => book.Id != id && batch.Contains(book.Series!) && book.SeriesNumber != null)
                .Select(book => book.SeriesNumber!.Value).ToArrayAsync(token));
        return new(row.Series, new(row.Id, row.Title, string.Join(", ", authors), row.SeriesNumber), numbers);
    }
}

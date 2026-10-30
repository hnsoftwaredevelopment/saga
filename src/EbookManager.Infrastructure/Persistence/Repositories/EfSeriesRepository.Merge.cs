using EbookManager.Domain.Series;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Infrastructure.Persistence.Repositories;

public sealed partial class EfSeriesRepository
{
    public async Task<SeriesMergePreview?> PreviewMergeAsync(IReadOnlyCollection<string> names, CancellationToken token)
    {
        var selected = names.Select(name => name.Trim()).Distinct(SeriesNames.Comparer).ToArray();
        if (selected.Length < 2 || selected.Any(string.IsNullOrWhiteSpace)) return null;
        await using var context = factory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        return await MergeSnapshotAsync(context, selected, token);
    }

    public async Task<SeriesMergeResult> MergeAsync(SeriesMergePreview preview, string targetName, CancellationToken token)
    {
        SeriesMergeResult Reject(SeriesMergeStatus status) => new(status, targetName, []);
        var selected = preview.Groups.Select(group => group.Name).ToArray();
        if (selected.Length < 2 || selected.Distinct(SeriesNames.Comparer).Count() != selected.Length ||
            selected.Any(string.IsNullOrWhiteSpace) || !selected.Contains(targetName, StringComparer.Ordinal)) return Reject(SeriesMergeStatus.InvalidSelection);
        await using var context = factory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        var current = await MergeSnapshotAsync(context, selected, token);
        if (current is null || current.Groups.Any(group => !preview.Groups.Any(old => SeriesNames.Same(old.Name, group.Name) &&
            old.Books.OrderBy(book => book.Id).SequenceEqual(group.Books)))) return Reject(SeriesMergeStatus.Conflict);
        var changedBooks = current.Groups.SelectMany(group => group.Books).Where(book => book.StoredName != targetName).ToArray();
        var updated = DateTimeOffset.UtcNow;
        foreach (var ids in changedBooks.Select(book => book.Id).Chunk(500))
        {
            var changed = await context.Books.Where(book => ids.Contains(book.Id)).ExecuteUpdateAsync(setters => setters
                .SetProperty(book => book.Series, targetName).SetProperty(book => book.UpdatedUtc, updated), token);
            if (changed != ids.Length) return Reject(SeriesMergeStatus.Conflict);
        }
        await transaction.CommitAsync(token);
        return new(SeriesMergeStatus.Merged, targetName, changedBooks);
    }

    private sealed record MergeRow(Guid Id, string StoredName, string Title, decimal? Number);
    private static async Task<SeriesMergePreview?> MergeSnapshotAsync(LibraryDbContext context, string[] selected, CancellationToken token)
    {
        var names = await NamesAsync(context, token);
        var groups = new List<SeriesMergeGroup>();
        foreach (var name in selected)
        {
            var rawNames = MatchingNames(names, name);
            if (rawNames.Length == 0) return null;
            var rows = new List<MergeRow>();
            foreach (var batch in rawNames.Chunk(500)) rows.AddRange(await context.Books.AsNoTracking().Where(book => batch.Contains(book.Series!))
                .Select(book => new MergeRow(book.Id, book.Series!, book.Title, book.SeriesNumber)).ToArrayAsync(token));
            var authors = new Dictionary<Guid, List<string>>();
            foreach (var batch in rows.Select(row => row.Id).Chunk(500))
            {
                var links = await context.BookAuthors.AsNoTracking().Where(link => batch.Contains(link.BookId)).OrderBy(link => link.Order)
                    .Select(link => new { link.BookId, link.Author.Name }).ToArrayAsync(token);
                foreach (var link in links)
                {
                    if (!authors.TryGetValue(link.BookId, out var values)) authors[link.BookId] = values = [];
                    values.Add(link.Name);
                }
            }
            groups.Add(new(name, rows.OrderBy(row => row.Id).Select(row => new SeriesMergeBook(row.Id, row.StoredName, row.Title,
                authors.TryGetValue(row.Id, out var values) ? string.Join(", ", values) : "", row.Number)).ToArray()));
        }
        return new(groups);
    }
}

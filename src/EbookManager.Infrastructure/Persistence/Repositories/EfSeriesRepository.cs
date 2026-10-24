using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Series;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Infrastructure.Persistence.Repositories;

public sealed partial class EfSeriesRepository(LibraryDbContextFactory factory, string libraryPath) : ISeriesRepository
{
    public async Task<SeriesPage> SearchAsync(string query, int skip, int take, CancellationToken token)
    {
        ValidatePage(skip, take);
        await using var context = factory.Create(libraryPath);
        var rows = await NamesAsync(context, token);
        var series = rows.Where(row => !string.IsNullOrWhiteSpace(row.Name))
            .GroupBy(row => row.Name.Trim(), SeriesNames.Comparer)
            .Select(group => new SeriesSummary(group.Select(row => row.Name.Trim()).OrderBy(name => name, StringComparer.Ordinal).First(), group.Sum(row => row.Count)))
            .Where(row => row.Name.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(row => row.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(row => row.Name, StringComparer.Ordinal).ToArray();
        token.ThrowIfCancellationRequested();
        return new(series.Skip(skip).Take(take).ToArray(), series.Length);
    }

    public async Task<SeriesBookPage> ListBooksAsync(string name, int skip, int take, CancellationToken token)
    {
        ValidatePage(skip, take);
        await using var context = factory.Create(libraryPath);
        var rawNames = MatchingNames(await NamesAsync(context, token), name);
        var keys = new List<BookKey>();
        foreach (var names in rawNames.Chunk(500))
            keys.AddRange(await context.Books.AsNoTracking().Where(book => names.Contains(book.Series!))
                .Select(book => new BookKey(book.Id, book.Title, book.SeriesNumber)).ToListAsync(token));
        // SQLite's stored decimal representation must not determine numeric ordering or page boundaries.
        var page = keys.OrderBy(book => book.Number is null).ThenBy(book => book.Number)
            .ThenBy(book => book.Title, StringComparer.CurrentCultureIgnoreCase).ThenBy(book => book.Id).Skip(skip).Take(take).ToArray();
        var ids = page.Select(book => book.Id).ToArray();
        var authors = await context.BookAuthors.AsNoTracking().Where(link => ids.Contains(link.BookId))
            .OrderBy(link => link.Order).Select(link => new { link.BookId, link.Author.Name }).ToListAsync(token);
        var byBook = authors.GroupBy(link => link.BookId).ToDictionary(group => group.Key, group => string.Join(", ", group.Select(link => link.Name)));
        return new(page.Select(book => new SeriesBook(book.Id, book.Title, byBook.GetValueOrDefault(book.Id, ""), book.Number)).ToArray(), keys.Count);
    }

    private sealed record NameCount(string Name, int Count);
    private sealed record BookKey(Guid Id, string Title, decimal? Number);
    private static Task<List<NameCount>> NamesAsync(LibraryDbContext context, CancellationToken token) => context.Books.AsNoTracking()
        .Where(book => book.Series != null).GroupBy(book => book.Series)
        .Select(group => new NameCount(group.Key!, group.Count())).ToListAsync(token);
    private static string[] MatchingNames(IEnumerable<NameCount> names, string name) => string.IsNullOrWhiteSpace(name) ? []
        : names.Where(row => SeriesNames.Same(row.Name, name)).Select(row => row.Name).ToArray();
    private static void ValidatePage(int skip, int take)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        if (take is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(take));
    }
}

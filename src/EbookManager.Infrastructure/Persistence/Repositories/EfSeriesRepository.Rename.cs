using EbookManager.Domain.Series;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Infrastructure.Persistence.Repositories;

public sealed partial class EfSeriesRepository
{
    public async Task<SeriesRenamePreview> PreviewRenameAsync(string name, CancellationToken token)
    {
        await using var context = factory.Create(libraryPath);
        return new(name, await SourceAsync(context, MatchingNames(await NamesAsync(context, token), name), token));
    }

    public async Task<SeriesRenameResult> RenameAsync(SeriesRenamePreview preview, string newName, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(preview);
        if (string.IsNullOrWhiteSpace(newName)) return new(SeriesRenameStatus.InvalidName, preview.Name, []);
        newName = newName.Trim();
        SeriesRenameResult Rejected(SeriesRenameStatus status) => new(status, newName, []);
        await using var context = factory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        var names = await NamesAsync(context, token);
        var books = await SourceAsync(context, MatchingNames(names, preview.Name), token);
        if (books.Length == 0 || books.Length != preview.Books.Count ||
            !books.Select(book => (book.Id, book.StoredName)).SequenceEqual(
                preview.Books.OrderBy(book => book.Id).Select(book => (book.Id, book.StoredName))))
            return Rejected(SeriesRenameStatus.Conflict);
        if (preview.SelectedBookIds is { } selectedIds)
        {
            var ids = selectedIds.ToHashSet();
            var selected = books.Where(book => ids.Contains(book.Id)).ToArray();
            if (selected.Length == 0 || selected.Length != selectedIds.Count ||
                (preview.Destination is null && selected.Length == books.Length) ||
                !selected.SequenceEqual(preview.Books.Where(book => ids.Contains(book.Id)).OrderBy(book => book.Id)))
                return Rejected(SeriesRenameStatus.Conflict);
            books = selected;
        }
        if (preview.Destination is { } destination)
        {
            if (preview.SelectedBookIds is null || newName != destination.Name || SeriesNames.Same(preview.Name, newName))
                return Rejected(SeriesRenameStatus.Conflict);
            var rawTargets = MatchingNames(names, newName);
            if (rawTargets.Length == 0 || rawTargets.Select(name => name.Trim()).Order(StringComparer.Ordinal).First() != destination.Name)
                return Rejected(SeriesRenameStatus.Conflict);
            var target = await SourceAsync(context, rawTargets, token);
            var current = SeriesMoveDestination.FromBooks(destination.Name, books, target);
            if (!current.OverlappingNumbers.SequenceEqual(destination.OverlappingNumbers)) return Rejected(SeriesRenameStatus.Conflict);
        }
        else if ((preview.SelectedBookIds is not null || !SeriesNames.Same(preview.Name, newName)) &&
            names.Any(row => SeriesNames.Same(row.Name, newName)))
            return Rejected(SeriesRenameStatus.NameExists);
        if (books.All(book => book.StoredName == newName)) return Rejected(SeriesRenameStatus.Unchanged);
        var updatedUtc = DateTimeOffset.UtcNow;
        foreach (var ids in books.Select(book => book.Id).Chunk(500))
        {
            var changed = await context.Books.Where(book => ids.Contains(book.Id)).ExecuteUpdateAsync(setters => setters
                .SetProperty(book => book.Series, newName).SetProperty(book => book.UpdatedUtc, updatedUtc), token);
            if (changed != ids.Length) return Rejected(SeriesRenameStatus.Conflict);
        }
        await transaction.CommitAsync(token);
        return new(SeriesRenameStatus.Renamed, newName, books);
    }

    private static async Task<SeriesSourceBook[]> SourceAsync(LibraryDbContext context, string[] rawNames, CancellationToken token)
    {
        var books = new List<SeriesSourceBook>();
        foreach (var names in rawNames.Chunk(500))
            books.AddRange(await context.Books.AsNoTracking().Where(book => names.Contains(book.Series!))
                .Select(book => new SeriesSourceBook(book.Id, book.Series!, book.Title) { Number = book.SeriesNumber }).ToListAsync(token));
        return books.OrderBy(book => book.Id).ToArray();
    }
}

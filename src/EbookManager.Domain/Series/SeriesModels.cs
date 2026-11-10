namespace EbookManager.Domain.Series;

public static class SeriesNames
{
    public static StringComparer Comparer => StringComparer.OrdinalIgnoreCase;
    public static bool Same(string? left, string? right) => Comparer.Equals(left?.Trim(), right?.Trim());
}

public sealed record SeriesSummary(string Name, int BookCount);
public sealed record SeriesPage(IReadOnlyList<SeriesSummary> Series, int TotalCount);
public sealed record SeriesBook(Guid Id, string Title, string Authors, decimal? Number);
public sealed record SeriesBookPage(IReadOnlyList<SeriesBook> Books, int TotalCount);

public sealed record SeriesSourceBook(Guid Id, string StoredName, string Title)
{
    public decimal? Number { get; init; }
}

public sealed record SeriesRenamePreview(string Name, IReadOnlyList<SeriesSourceBook> Books)
{
    // A null selection renames the entire series. A move to a new name leaves at least one book behind.
    public IReadOnlyList<Guid>? SelectedBookIds { get; init; }
    public SeriesMoveDestination? Destination { get; init; }
}

public sealed record SeriesMoveDestination(string Name, IReadOnlyList<decimal> OverlappingNumbers)
{
    public static SeriesMoveDestination FromBooks(string name, IEnumerable<SeriesSourceBook> moving, IEnumerable<SeriesSourceBook> target)
    {
        var numbers = moving.Select(book => book.Number).OfType<decimal>();
        var otherNumbers = target.Select(book => book.Number).OfType<decimal>();
        return new(name, numbers.Intersect(otherNumbers).Order().ToArray());
    }
}
public enum SeriesRenameStatus { Renamed, Unchanged, InvalidName, NameExists, Conflict }
public sealed record SeriesRenameResult(SeriesRenameStatus Status, string Name, IReadOnlyList<SeriesSourceBook> Books);

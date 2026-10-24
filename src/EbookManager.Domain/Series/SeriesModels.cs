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

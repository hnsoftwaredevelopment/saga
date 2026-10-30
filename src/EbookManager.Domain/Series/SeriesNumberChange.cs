namespace EbookManager.Domain.Series;

public sealed record SeriesNumberPreview(string StoredName, SeriesBook Book, IReadOnlyList<decimal> OtherNumbers);
public enum SeriesNumberStatus { Changed, Unchanged, Conflict }
public sealed record SeriesNumberResult(SeriesNumberStatus Status, int MatchingOtherBooks);

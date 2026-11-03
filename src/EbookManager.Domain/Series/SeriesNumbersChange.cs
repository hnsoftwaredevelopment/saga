namespace EbookManager.Domain.Series;

public sealed record SeriesNumberSource(string StoredName, SeriesBook Book);
public sealed record SeriesNumbersPreview(string Name, IReadOnlyList<SeriesNumberSource> Books, IReadOnlyList<decimal> OtherNumbers);
public sealed record SeriesNumberAssignment(Guid Id, decimal Number);
public sealed record SeriesNumbersResult(SeriesNumberStatus Status, IReadOnlyList<SeriesSourceBook> Books, int MatchingOtherBooks);

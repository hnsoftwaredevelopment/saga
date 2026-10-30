namespace EbookManager.Domain.Series;

public sealed record SeriesMergeBook(Guid Id, string StoredName, string Title, string Authors, decimal? Number);
public sealed record SeriesMergeGroup(string Name, IReadOnlyList<SeriesMergeBook> Books);
public sealed record SeriesMergePreview(IReadOnlyList<SeriesMergeGroup> Groups)
{
    public int BookCount => Groups.SelectMany(group => group.Books).Select(book => book.Id).Distinct().Count();
}
public enum SeriesMergeStatus { Merged, InvalidSelection, Conflict }
public sealed record SeriesMergeResult(SeriesMergeStatus Status, string Name, IReadOnlyList<SeriesMergeBook> Books);

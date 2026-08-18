namespace EbookManager.Domain.Books;

public readonly record struct DuplicateExclusionPair(Guid FirstBookId, Guid SecondBookId)
{
    public static DuplicateExclusionPair Create(Guid firstBookId, Guid secondBookId) =>
        firstBookId.CompareTo(secondBookId) <= 0
            ? new DuplicateExclusionPair(firstBookId, secondBookId)
            : new DuplicateExclusionPair(secondBookId, firstBookId);
}

namespace EbookManager.Domain.Books;

public sealed record DuplicateExclusion(
    DuplicateExclusionPair Pair,
    string FirstBookTitle,
    IReadOnlyList<string> FirstBookAuthors,
    string SecondBookTitle,
    IReadOnlyList<string> SecondBookAuthors,
    DateTimeOffset CreatedAt);

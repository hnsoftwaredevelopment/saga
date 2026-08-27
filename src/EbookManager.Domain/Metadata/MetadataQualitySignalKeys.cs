namespace EbookManager.Domain.Metadata;

public static class MetadataQualitySignalKeys
{
    public const string MissingAuthor = "missing-author";
    public const string UnknownLanguage = "unknown-language";
    public const string MissingCover = "missing-cover";
    public const string SeriesNumberWithoutSeries = "series-number-without-series";
    public const string PossibleTitleAuthorSwap = "possible-title-author-swap";
    public const string MessyTags = "messy-tags";

    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(
    [
        MissingAuthor,
        UnknownLanguage,
        MissingCover,
        SeriesNumberWithoutSeries,
        PossibleTitleAuthorSwap,
        MessyTags
    ]);
}

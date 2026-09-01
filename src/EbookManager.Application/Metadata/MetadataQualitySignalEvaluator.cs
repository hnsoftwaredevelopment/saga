using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;

namespace EbookManager.Application.Metadata;

public static class MetadataQualitySignalEvaluator
{
    public static IReadOnlySet<string> Evaluate(Book book)
    {
        ArgumentNullException.ThrowIfNull(book);

        var signals = new HashSet<string>(StringComparer.Ordinal);
        AddIf(signals, MetadataQualitySignalKeys.MissingAuthor, HasMissingAuthor(book));
        AddIf(signals, MetadataQualitySignalKeys.UnknownLanguage, HasUnknownLanguage(book));
        AddIf(signals, MetadataQualitySignalKeys.MissingCover, HasMissingCover(book));
        AddIf(signals, MetadataQualitySignalKeys.SeriesNumberWithoutSeries, HasSeriesNumberWithoutSeries(book));
        AddIf(signals, MetadataQualitySignalKeys.PossibleTitleAuthorSwap, HasPossibleTitleAuthorSwap(book));
        AddIf(signals, MetadataQualitySignalKeys.MessyTags, HasMessyTags(book));
        return signals;
    }

    public static bool Applies(Book book, string signalKey) =>
        Evaluate(book).Contains(signalKey);

    private static void AddIf(ISet<string> signals, string signalKey, bool applies)
    {
        if (applies)
        {
            signals.Add(signalKey);
        }
    }

    private static bool HasMissingAuthor(Book book) =>
        !book.Metadata.Authors.Any(MetadataQualityAuthorRules.IsUsable);

    private static bool HasUnknownLanguage(Book book)
    {
        var language = book.Metadata.Language;
        if (string.IsNullOrWhiteSpace(language))
        {
            return true;
        }

        var key = LanguageDisplayService.FilterKey(language);
        if (string.IsNullOrWhiteSpace(key))
        {
            return true;
        }

        try
        {
            _ = System.Globalization.CultureInfo.GetCultureInfo(key);
            return false;
        }
        catch (System.Globalization.CultureNotFoundException)
        {
            return true;
        }
    }

    private static bool HasMissingCover(Book book) =>
        book.Metadata.CoverBytes is null &&
        string.IsNullOrWhiteSpace(book.CoverRelativePath);

    private static bool HasSeriesNumberWithoutSeries(Book book) =>
        book.Metadata.SeriesNumber is not null &&
        string.IsNullOrWhiteSpace(book.Metadata.Series);

    private static bool HasPossibleTitleAuthorSwap(Book book)
    {
        if (book.Metadata.Authors.Count != 1)
        {
            return false;
        }

        var title = book.Metadata.Title.Trim();
        var author = book.Metadata.Authors[0].Trim();
        return LooksLikePersonName(title) && !LooksLikePersonName(author);
    }

    private static bool HasMessyTags(Book book)
    {
        var tags = book.Metadata.Tags ?? [];
        return tags.Any(tag =>
            string.IsNullOrWhiteSpace(tag) ||
            tag != tag.Trim() ||
            tag.Contains("  ", StringComparison.Ordinal) ||
            tag.Contains(',', StringComparison.Ordinal));
    }

    private static bool LooksLikePersonName(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length is >= 2 and <= 4 &&
            parts.All(part => part.Length > 1 && char.IsUpper(part[0]) && part.Skip(1).Any(char.IsLower));
    }
}

using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;

namespace EbookManager.Application.Metadata;

public static class MetadataQualitySignalEvaluator
{
    private static readonly HashSet<string> NameParticles = new(StringComparer.OrdinalIgnoreCase)
    {
        "van", "von", "de", "den", "der", "del", "della", "di", "da", "dos", "du",
        "le", "la", "ten", "ter", "te", "el", "al", "bin", "ibn"
    };

    private static readonly HashSet<string> TitleArticles = new(StringComparer.OrdinalIgnoreCase)
    {
        "de", "het", "een", "the", "a", "an", "der", "die", "das", "ein", "eine",
        "le", "la", "les", "un", "une", "el", "los", "las", "una", "uno", "il", "lo", "gli"
    };

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
        // Capitalized words alone are too weak: require independent title-like evidence
        // in the author field. Ambiguous cases remain available through the detail action.
        return MetadataQualityAuthorRules.IsUsable(author) &&
            LooksLikePersonName(title) && !LooksLikePersonName(author) && LooksLikeBookTitle(author);
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
        if (value.Count(character => character == ',') > 1)
        {
            return false;
        }

        var parts = SplitWords(value.Replace(',', ' '));
        return parts.Length is >= 2 and <= 8 &&
            parts.Count(part => !NameParticles.Contains(part)) >= 2 &&
            parts.All(part => NameParticles.Contains(part) ||
                (char.IsUpper(part[0]) && part.All(character =>
                    char.IsLetter(character) || character is '.' or '-' or '\'' or '’')));
    }

    private static bool LooksLikeBookTitle(string value)
    {
        var parts = SplitWords(value);
        return parts.Length >= 3 && TitleArticles.Contains(parts[0]) &&
            parts.Count(part => char.IsLower(part[0]) && !NameParticles.Contains(part)) >= 2;
    }

    private static string[] SplitWords(string value) =>
        value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
}

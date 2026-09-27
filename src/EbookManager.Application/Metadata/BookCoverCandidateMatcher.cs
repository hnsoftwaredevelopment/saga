using System.Globalization;
using System.Text;

namespace EbookManager.Application.Metadata;

public static class BookCoverCandidateMatcher
{
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "a", "an", "and", "de", "den", "der", "een", "en", "et", "het", "la", "le", "of", "the", "van"
    };

    public static int Score(BookCoverSearchQuery query, BookCoverCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(candidate);

        if (IsbnValidator.TryNormalize(query.Isbn, out var queryIsbn) &&
            candidate.Isbns?.Any(value =>
                IsbnValidator.TryNormalize(value, out var candidateIsbn) &&
                string.Equals(queryIsbn, candidateIsbn, StringComparison.Ordinal)) == true)
        {
            return 1_000;
        }

        var queryTitle = Normalize(query.Title);
        var candidateTitle = Normalize(candidate.Title);
        if (queryTitle.Length == 0 || candidateTitle.Length == 0)
        {
            return 0;
        }

        var exactTitle = string.Equals(queryTitle, candidateTitle, StringComparison.Ordinal);
        var queryTitleTokens = Tokens(query.Title);
        var candidateTitleTokens = Tokens(candidate.Title).ToHashSet(StringComparer.Ordinal);
        if (!exactTitle && !HasStrongCoverage(queryTitleTokens, candidateTitleTokens))
        {
            return 0;
        }

        var queryAuthorTokens = query.Authors.SelectMany(Tokens).ToHashSet(StringComparer.Ordinal);
        var candidateAuthorTokens = candidate.Authors.SelectMany(Tokens).ToHashSet(StringComparer.Ordinal);
        if (candidateAuthorTokens.Count == 0)
        {
            return exactTitle ? 500 : 0;
        }

        if (queryAuthorTokens.Count > 0 && !queryAuthorTokens.Overlaps(candidateAuthorTokens))
        {
            return 0;
        }

        var titleMatches = queryTitleTokens.Count(candidateTitleTokens.Contains);
        var authorMatches = queryAuthorTokens.Count(candidateAuthorTokens.Contains);
        return (exactTitle ? 500 : 300) + titleMatches * 10 + authorMatches * 20;
    }

    public static IReadOnlyList<string> Tokens(string? value) =>
        Normalize(value)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length > 1 && !StopWords.Contains(token))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static bool HasStrongCoverage(IReadOnlyList<string> queryTokens, IReadOnlySet<string> candidateTokens)
    {
        if (queryTokens.Count == 0)
        {
            return false;
        }

        var required = queryTokens.Count == 1 ? 1 : (int)Math.Ceiling(queryTokens.Count * 0.7);
        return queryTokens.Count(candidateTokens.Contains) >= required;
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var previousWasSpace = true;
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSpace = false;
            }
            else if (!previousWasSpace)
            {
                builder.Append(' ');
                previousWasSpace = true;
            }
        }

        return builder.ToString().Trim();
    }
}

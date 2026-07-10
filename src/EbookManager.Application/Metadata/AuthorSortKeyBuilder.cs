using EbookManager.Domain.Settings;

namespace EbookManager.Application.Metadata;

public static class AuthorSortKeyBuilder
{
    private static readonly string[] DutchPrefixes =
    [
        "van",
        "de",
        "den",
        "der",
        "van de",
        "van der",
        "van den"
    ];

    public static string BuildSortKey(string? authorsText, AuthorSortStrategy strategy)
    {
        var author = FirstAuthor(authorsText);
        if (string.IsNullOrWhiteSpace(author) || strategy == AuthorSortStrategy.DisplayName)
        {
            return author;
        }

        if (author.Contains(',', StringComparison.Ordinal))
        {
            return author;
        }

        var parts = author.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            return author;
        }

        return strategy == AuthorSortStrategy.LastNameFirstDutchPrefixes
            ? BuildDutchPrefixAwareKey(parts)
            : $"{parts[^1]}, {string.Join(' ', parts[..^1])}";
    }

    private static string FirstAuthor(string? authorsText) =>
        (authorsText ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? string.Empty;

    private static string BuildDutchPrefixAwareKey(string[] parts)
    {
        var lastNameStart = parts.Length - 1;
        for (var prefixLength = Math.Min(3, parts.Length - 1); prefixLength >= 1; prefixLength--)
        {
            var candidateStart = parts.Length - 1 - prefixLength;
            var candidate = string.Join(' ', parts[candidateStart..^1]);
            if (DutchPrefixes.Contains(candidate, StringComparer.OrdinalIgnoreCase))
            {
                lastNameStart = candidateStart;
                break;
            }
        }

        var lastName = string.Join(' ', parts[lastNameStart..]);
        var firstNames = string.Join(' ', parts[..lastNameStart]);
        return string.IsNullOrWhiteSpace(firstNames) ? lastName : $"{lastName}, {firstNames}";
    }
}

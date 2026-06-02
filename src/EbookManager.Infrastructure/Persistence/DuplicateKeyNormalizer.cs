using System.Text;

namespace EbookManager.Infrastructure.Persistence;

internal static class DuplicateKeyNormalizer
{
    internal static string NormalizeSqliteText(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return LowerAsciiOnly(TrimSpaces(value));
    }

    internal static string BuildDuplicateKey(string title, IReadOnlyList<string> authors)
    {
        var normalizedTitle = NormalizeSqliteText(title);
        var normalizedAuthors = NormalizeAuthors(authors);

        return $"T1:{EncodeComponent(normalizedTitle)}|A{normalizedAuthors.Count}:{string.Join('|', normalizedAuthors.Select(EncodeComponent))}";
    }

    private static IReadOnlyList<string> NormalizeAuthors(IReadOnlyList<string> authors)
    {
        var normalizedAuthors = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var author in authors)
        {
            var normalizedAuthor = NormalizeSqliteText(author);
            if (normalizedAuthor.Length == 0)
            {
                continue;
            }

            if (seen.Add(normalizedAuthor))
            {
                normalizedAuthors.Add(normalizedAuthor);
            }
        }

        normalizedAuthors.Sort(StringComparer.Ordinal);
        return normalizedAuthors;
    }

    private static string EncodeComponent(string value) =>
        $"{Encoding.UTF8.GetByteCount(value)}:{value}";

    private static string TrimSpaces(string value)
    {
        var start = 0;
        var end = value.Length - 1;

        while (start < value.Length && value[start] == ' ')
        {
            start++;
        }

        while (end >= start && value[end] == ' ')
        {
            end--;
        }

        return start == 0 && end == value.Length - 1
            ? value
            : value[start..(end + 1)];
    }

    private static string LowerAsciiOnly(string value)
    {
        var chars = value.ToCharArray();
        for (var index = 0; index < chars.Length; index++)
        {
            var character = chars[index];
            if (character is >= 'A' and <= 'Z')
            {
                chars[index] = (char)(character + ('a' - 'A'));
            }
        }

        return new string(chars);
    }
}

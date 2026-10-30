using EbookManager.Domain.Authors;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EbookManager.Application.Authors;

public enum AuthorDuplicateReason { NameFormatting, MatchingInitials }
public sealed record AuthorDuplicateCandidate(AuthorSummary First, AuthorSummary Second, AuthorDuplicateReason Reason);
public sealed record AuthorDuplicateResult(IReadOnlyList<AuthorDuplicateCandidate> Candidates, bool Truncated);

public static class AuthorDuplicateFinder
{
    private sealed record Name(AuthorSummary Author, string Surname, string[] Given, string Search);
    private static readonly HashSet<string> Prefixes = ["van", "de", "den", "der", "ten", "ter", "von", "da", "di", "la", "le", "du", "dos", "del"];
    public static AuthorDuplicateResult Find(IReadOnlyList<AuthorSummary> authors, string query = "", int limit = 1000, CancellationToken token = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        token.ThrowIfCancellationRequested();
        var names = new List<Name>();
        foreach (var author in authors.DistinctBy(author => author.Id))
        {
            token.ThrowIfCancellationRequested();
            if (Parse(author) is { } name) names.Add(name);
        }
        var filter = Fold(query).Trim();
        var result = new List<AuthorDuplicateCandidate>();
        foreach (var bucket in names.OrderBy(name => name.Search, StringComparer.Ordinal).ThenBy(name => name.Author.Id)
            .GroupBy(name => (name.Surname, Initial: name.Given.Length == 0 ? "" : name.Given[0][..1])))
        {
            var group = bucket.ToArray();
            if (filter.Length > 0 && !group.Any(name => name.Search.Contains(filter, StringComparison.Ordinal))) continue;
            for (var first = 0; first < group.Length; first++)
                for (var second = first + 1; second < group.Length; second++)
                {
                    token.ThrowIfCancellationRequested();
                    var left = group[first]; var right = group[second];
                    if (filter.Length > 0 && !left.Search.Contains(filter, StringComparison.Ordinal) && !right.Search.Contains(filter, StringComparison.Ordinal)) continue;
                    var reason = Compare(left, right);
                    if (reason is null) continue;
                    if (result.Count == limit) return new(result, true);
                    result.Add(new(left.Author, right.Author, reason.Value));
                }
        }
        return new(result, false);
    }

    private static AuthorDuplicateReason? Compare(Name left, Name right)
    {
        if (left.Given.SequenceEqual(right.Given, StringComparer.Ordinal)) return AuthorDuplicateReason.NameFormatting;
        if (left.Given.Length == 0 || left.Given.Length != right.Given.Length) return null;
        for (var index = 0; index < left.Given.Length; index++)
        {
            var a = left.Given[index]; var b = right.Given[index];
            if (a == b) continue;
            if (a.Length == 1 && b.StartsWith(a, StringComparison.Ordinal) || b.Length == 1 && a.StartsWith(b, StringComparison.Ordinal)) continue;
            return null;
        }
        return AuthorDuplicateReason.MatchingInitials;
    }

    private static Name? Parse(AuthorSummary author)
    {
        var text = Fold(author.Name).Trim();
        if (string.IsNullOrWhiteSpace(text) || text == "unknown" || text.Any(character => char.IsDigit(character) || char.IsControl(character) || character is ';' or '@' or '&' or '/' or '\\')) return null;
        var spaced = Regex.Replace(text, @"(?<=\p{L})\.(?=\p{L})", " ");
        var comma = spaced.Split(',');
        string surname;
        string[] given;
        if (comma.Length == 2)
        {
            surname = string.Concat(Words(comma[0]));
            given = Words(comma[1]);
        }
        else if (comma.Length == 1)
        {
            var words = Words(spaced);
            if (words.Length == 0) return null;
            var start = words.Length - 1;
            while (start > 0 && Prefixes.Contains(words[start - 1])) start--;
            surname = string.Concat(words[start..]);
            given = words[..start];
        }
        else return null;
        return surname.Length == 0 ? null : new(author, surname, given, text);
    }
    private static string[] Words(string value) => Regex.Matches(value, @"\p{L}+(?:[-'’]\p{L}+)*")
        .Select(match => string.Concat(match.Value.Where(char.IsLetter))).Where(word => word.Length > 0).ToArray();
    private static string Fold(string value)
    {
        var result = new StringBuilder(value.Length);
        foreach (var character in value.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark) result.Append(char.ToLowerInvariant(character));
        return result.ToString();
    }
}

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EbookManager.Domain.Books;

namespace EbookManager.Application.Books;

public sealed partial class DuplicateCandidateService
{
    public DuplicateCandidateResult FindCandidates(IReadOnlyList<Book> books)
    {
        var groups = books
            .Select(book => new DuplicateCandidateBook(book, NormalizeTitle(book.Metadata.Title)))
            .Where(item => item.Key.Length > 0)
            .GroupBy(item => item.Key, StringComparer.Ordinal)
            .SelectMany(CreateAuthorMatchedGroups)
            .Select(group => new DuplicateCandidateGroup(
                group.Key,
                group.Books[0].Metadata.Title.Trim(),
                FormatAuthorSummary(group.Books),
                group.Books,
                group.MatchKind))
            .OrderBy(group => group.DisplayTitle, StringComparer.CurrentCultureIgnoreCase)
            .ToList()
            .AsReadOnly();

        return new DuplicateCandidateResult(groups);
    }

    private static IEnumerable<AuthorMatchedGroup> CreateAuthorMatchedGroups(
        IGrouping<string, DuplicateCandidateBook> titleGroup)
    {
        var titleBooks = titleGroup
            .Select(item => item.Book)
            .ToList();
        var remaining = titleBooks.ToList();
        var authorMatchedGroups = new List<AuthorMatchedGroup>();

        var componentIndex = 0;
        while (remaining.Count > 0)
        {
            var seed = remaining[0];
            remaining.RemoveAt(0);
            var component = new List<Book> { seed };
            for (var index = 0; index < component.Count; index++)
            {
                var current = component[index];
                for (var remainingIndex = remaining.Count - 1; remainingIndex >= 0; remainingIndex--)
                {
                    if (!SharesAuthor(current, remaining[remainingIndex]))
                    {
                        continue;
                    }

                    component.Add(remaining[remainingIndex]);
                    remaining.RemoveAt(remainingIndex);
                }
            }

            if (component.Count > 1)
            {
                authorMatchedGroups.Add(new AuthorMatchedGroup(
                    $"{titleGroup.Key}:{componentIndex++}",
                    component.AsReadOnly(),
                    DuplicateCandidateMatchKind.AuthorOverlap));
            }
        }

        if (authorMatchedGroups.Count > 0)
        {
            foreach (var group in authorMatchedGroups)
            {
                yield return group;
            }

            yield break;
        }

        if (titleBooks.Count > 1)
        {
            yield return new AuthorMatchedGroup(
                $"{titleGroup.Key}:title",
                titleBooks.AsReadOnly(),
                DuplicateCandidateMatchKind.TitleOnly);
        }
    }

    private static bool SharesAuthor(Book first, Book second)
    {
        var firstAuthors = first.Metadata.Authors
            .Select(NormalizeAuthor)
            .Where(author => author.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
        return second.Metadata.Authors
            .Select(NormalizeAuthor)
            .Any(firstAuthors.Contains);
    }

    private static string NormalizeAuthor(string author) =>
        WhitespaceRegex().Replace(author.Trim().ToLowerInvariant(), " ");

    private static string FormatAuthorSummary(IReadOnlyList<Book> books) =>
        string.Join(
            ", ",
            books
                .SelectMany(book => book.Metadata.Authors)
                .Where(author => !string.IsNullOrWhiteSpace(author))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(author => author, StringComparer.CurrentCultureIgnoreCase));

    private static string NormalizeTitle(string title)
    {
        var normalized = title.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        var withoutDiacritics = builder.ToString().Normalize(NormalizationForm.FormC);
        return WhitespaceRegex().Replace(NonWordRegex().Replace(withoutDiacritics, " "), " ").Trim();
    }

    [GeneratedRegex(@"[^\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex NonWordRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}

public sealed record DuplicateCandidateResult(IReadOnlyList<DuplicateCandidateGroup> Groups);

public sealed record DuplicateCandidateGroup(
    string MatchKey,
    string DisplayTitle,
    string AuthorSummary,
    IReadOnlyList<Book> Books,
    DuplicateCandidateMatchKind MatchKind = DuplicateCandidateMatchKind.AuthorOverlap);

public enum DuplicateCandidateMatchKind
{
    AuthorOverlap,
    TitleOnly
}

internal sealed record AuthorMatchedGroup(
    string Key,
    IReadOnlyList<Book> Books,
    DuplicateCandidateMatchKind MatchKind);

internal sealed record DuplicateCandidateBook(Book Book, string Key);

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
            .Select(book => new { Book = book, Key = NormalizeTitle(book.Metadata.Title) })
            .Where(item => item.Key.Length > 0)
            .GroupBy(item => item.Key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => new DuplicateCandidateGroup(
                group.Key,
                group.First().Book.Metadata.Title.Trim(),
                group.Select(item => item.Book).ToList().AsReadOnly()))
            .OrderBy(group => group.DisplayTitle, StringComparer.CurrentCultureIgnoreCase)
            .ToList()
            .AsReadOnly();

        return new DuplicateCandidateResult(groups);
    }

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
    IReadOnlyList<Book> Books);

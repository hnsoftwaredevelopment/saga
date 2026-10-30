using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Series;
using System.Text;

namespace EbookManager.Application.Series;

public sealed record SeriesDuplicateCandidate(SeriesSummary First, SeriesSummary Second);
public sealed record SeriesDuplicateResult(IReadOnlyList<SeriesDuplicateCandidate> Candidates, bool Truncated);
public static class SeriesDuplicateFinder
{
    public static SeriesDuplicateResult Find(IReadOnlyList<SeriesSummary> series, string query = "", int limit = 1000, CancellationToken token = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        token.ThrowIfCancellationRequested();
        var groups = new Dictionary<string, List<SeriesSummary>>(StringComparer.Ordinal);
        foreach (var item in series.Where(item => !string.IsNullOrWhiteSpace(item.Name)).DistinctBy(item => item.Name.Trim(), SeriesNames.Comparer))
        {
            token.ThrowIfCancellationRequested();
            var key = Key(item.Name);
            if (key.Length == 0) continue;
            if (!groups.TryGetValue(key, out var names)) groups[key] = names = [];
            names.Add(item);
        }
        var result = new List<SeriesDuplicateCandidate>();
        var filter = query.Trim();
        foreach (var group in groups.Values)
        {
            var matches = group.Select((row, index) => (row, index)).Where(item => item.row.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).Select(item => item.index).ToArray();
            if (matches.Length == 0) continue;
            var matching = matches.ToHashSet();
            for (var first = 0; first < group.Count; first++)
                foreach (var second in matching.Contains(first) ? Enumerable.Range(first + 1, group.Count - first - 1) : matches.Where(index => index > first))
                {
                    token.ThrowIfCancellationRequested();
                    if (result.Count == limit) return new(result, true);
                    result.Add(new(group[first], group[second]));
                }
        }
        return new(result, false);
    }
    private static string Key(string value)
    {
        var key = new StringBuilder(value.Length);
        foreach (var character in value.Normalize(NormalizationForm.FormD).EnumerateRunes())
            if (Rune.IsLetter(character) || Rune.IsNumber(character)) key.Append(Rune.ToLowerInvariant(character));
        return key.ToString();
    }
}
public interface ISeriesDuplicateIndex { Task<IReadOnlyList<SeriesSummary>> LoadAsync(CancellationToken token); }
public sealed class SeriesDuplicateSearchService(ISeriesRepository repository) : ISeriesDuplicateIndex
{
    public async Task<IReadOnlyList<SeriesSummary>> LoadAsync(CancellationToken token)
    {
        var rows = new List<SeriesSummary>(); var skip = 0; SeriesPage page;
        do
        {
            token.ThrowIfCancellationRequested(); page = await repository.SearchAsync("", skip, 500, token);
            rows.AddRange(page.Series); skip += page.Series.Count;
        } while (page.Series.Count > 0 && skip < page.TotalCount);
        return rows.DistinctBy(row => row.Name.Trim(), SeriesNames.Comparer).ToArray();
    }
}

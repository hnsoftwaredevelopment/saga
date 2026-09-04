namespace EbookManager.Application.Metadata;

public interface IBookCoverSearchService
{
    Task<BookCoverSearchResult> SearchAsync(
        BookCoverSearchQuery query,
        CancellationToken cancellationToken);

    Task<BookCoverDownloadResult> DownloadAsync(
        BookCoverCandidate candidate,
        CancellationToken cancellationToken);
}

public interface IBookCoverSource
{
    string SourceKey { get; }

    Task<BookCoverSearchResult> SearchAsync(
        BookCoverSearchQuery query,
        CancellationToken cancellationToken);

    Task<BookCoverDownloadResult> DownloadAsync(
        string candidateId,
        CancellationToken cancellationToken);
}

public sealed record BookCoverSearchQuery(
    string Title,
    IReadOnlyList<string> Authors,
    string? Isbn);

public sealed record BookCoverCandidate(
    string SourceKey,
    string CandidateId,
    string Source,
    string Title,
    IReadOnlyList<string> Authors,
    byte[] PreviewBytes,
    int Width,
    int Height);

public sealed record BookCoverSearchResult(
    BookCoverSearchStatus Status,
    IReadOnlyList<BookCoverCandidate> Candidates,
    string? Message = null);

public enum BookCoverSearchStatus
{
    Succeeded,
    NoResults,
    Failed
}

public sealed record BookCoverDownloadResult(
    BookCoverDownloadStatus Status,
    byte[]? Bytes = null,
    int? Width = null,
    int? Height = null,
    string? Message = null);

public enum BookCoverDownloadStatus
{
    Succeeded,
    InvalidCandidate,
    Failed
}

public sealed class CompositeBookCoverSearchService(
    IEnumerable<IBookCoverSource> sources,
    IBookCoverSource? fallbackSource = null) : IBookCoverSearchService
{
    private const int MaximumCandidates = 12;
    private readonly IReadOnlyList<IBookCoverSource> sources = sources.ToArray();
    private readonly IBookCoverSource? fallbackSource = fallbackSource;

    public async Task<BookCoverSearchResult> SearchAsync(
        BookCoverSearchQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var results = await Task.WhenAll(sources.Select(source => SearchSourceAsync(source, query, cancellationToken)));
        var validLists = results
            .Select((result, index) => result.Candidates
                .Where(candidate => string.Equals(candidate.SourceKey, sources[index].SourceKey, StringComparison.Ordinal))
                .ToArray())
            .ToArray();
        var candidates = Interleave(validLists, MaximumCandidates);

        if (candidates.Count == 0 && fallbackSource is not null)
        {
            var fallback = await SearchSourceAsync(fallbackSource, query, cancellationToken);
            candidates = fallback.Candidates
                .Where(candidate => string.Equals(candidate.SourceKey, fallbackSource.SourceKey, StringComparison.Ordinal))
                .Take(1)
                .ToArray();
        }

        if (candidates.Count > 0)
        {
            return new(BookCoverSearchStatus.Succeeded, candidates);
        }

        return new(
            results.Length > 0 && results.All(result => result.Status == BookCoverSearchStatus.Failed)
                ? BookCoverSearchStatus.Failed
                : BookCoverSearchStatus.NoResults,
            []);
    }

    public Task<BookCoverDownloadResult> DownloadAsync(
        BookCoverCandidate candidate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var source = sources
            .Append(fallbackSource)
            .FirstOrDefault(value => value is not null &&
                string.Equals(value.SourceKey, candidate.SourceKey, StringComparison.Ordinal));
        return source is null || string.IsNullOrWhiteSpace(candidate.CandidateId)
            ? Task.FromResult(new BookCoverDownloadResult(BookCoverDownloadStatus.InvalidCandidate))
            : source.DownloadAsync(candidate.CandidateId, cancellationToken);
    }

    private static async Task<BookCoverSearchResult> SearchSourceAsync(
        IBookCoverSource source,
        BookCoverSearchQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            return await source.SearchAsync(query, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new(BookCoverSearchStatus.Failed, [], exception.Message);
        }
    }

    private static IReadOnlyList<BookCoverCandidate> Interleave(
        IReadOnlyList<BookCoverCandidate>[] lists,
        int maximum)
    {
        var result = new List<BookCoverCandidate>(maximum);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var offset = 0; result.Count < maximum && lists.Any(list => offset < list.Count); offset++)
        {
            foreach (var candidate in lists.Where(list => offset < list.Count).Select(list => list[offset]))
            {
                if (seen.Add(candidate.SourceKey + "\0" + candidate.CandidateId))
                {
                    result.Add(candidate);
                    if (result.Count == maximum)
                    {
                        break;
                    }
                }
            }
        }

        return result;
    }
}

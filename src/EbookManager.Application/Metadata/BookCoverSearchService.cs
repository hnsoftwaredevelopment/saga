namespace EbookManager.Application.Metadata;

public interface IBookCoverSearchService
{
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
    long CoverId,
    string Source,
    string Title,
    IReadOnlyList<string> Authors,
    byte[] PreviewBytes,
    int Width,
    int Height)
{
    public string CandidateId => CoverId.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

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

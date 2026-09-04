using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using EbookManager.Application.Metadata;

namespace EbookManager.Infrastructure.Metadata;

public sealed class OpenLibraryBookCoverSearchService(
    HttpClient httpClient,
    IBookCoverImageValidator imageValidator) : IBookCoverSource
{
    public const string Key = "open-library";
    private const int MaximumCandidates = 12;
    private const int SearchResultLimit = 24;
    private const int MinimumDimension = 50;
    private const int MaximumDimension = 8_000;
    private const long MaximumPixelCount = 25_000_000;
    private const int MaximumJsonBytes = 1024 * 1024;
    private const int MaximumPreviewImageBytes = 2 * 1024 * 1024;
    private const int MaximumImageBytes = 10 * 1024 * 1024;
    private const string SourceName = "Open Library";
    private static readonly TimeSpan SearchTimeout = TimeSpan.FromSeconds(15);

    private readonly HttpClient httpClient = httpClient;
    private readonly IBookCoverImageValidator imageValidator = imageValidator;

    public string SourceKey => Key;

    public async Task<BookCoverSearchResult> SearchAsync(
        BookCoverSearchQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        using var searchCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        searchCancellation.CancelAfter(SearchTimeout);
        var searchToken = searchCancellation.Token;

        try
        {
            var rows = new List<SearchRow>();
            var requestFailed = false;
            foreach (var requestUri in CreateSearchUris(query))
            {
                try
                {
                    rows.AddRange(await SearchRowsAsync(requestUri, searchToken));
                }
                catch (Exception exception) when (IsExpectedExternalFailure(exception, cancellationToken))
                {
                    requestFailed = true;
                }
            }

            var uniqueRows = rows
                .Where(row => row.CoverId > 0)
                .Select(row => (Row: row, Score: BookCoverCandidateMatcher.Score(query, ToMetadataCandidate(row))))
                .Where(value => value.Score > 0)
                .OrderByDescending(value => value.Score)
                .Select(value => value.Row)
                .DistinctBy(row => row.CoverId)
                .Take(SearchResultLimit)
                .ToArray();
            if (uniqueRows.Length == 0)
            {
                return new(
                    requestFailed ? BookCoverSearchStatus.Failed : BookCoverSearchStatus.NoResults,
                    []);
            }

            var candidates = new List<BookCoverCandidate>(MaximumCandidates);
            foreach (var row in uniqueRows)
            {
                searchToken.ThrowIfCancellationRequested();
                var image = await TryDownloadImageAsync(
                    row.CoverId,
                    "M",
                    MaximumPreviewImageBytes,
                    searchToken);
                if (image is null)
                {
                    continue;
                }

                candidates.Add(new BookCoverCandidate(
                    Key,
                    row.CoverId.ToString(CultureInfo.InvariantCulture),
                    SourceName,
                    row.Title,
                    row.Authors,
                    image.Value.Bytes,
                    image.Value.Width,
                    image.Value.Height,
                    row.Isbns));
                if (candidates.Count == MaximumCandidates)
                {
                    break;
                }
            }

            if (candidates.Count == 0)
            {
                return new(BookCoverSearchStatus.NoResults, []);
            }

            var ordered = candidates
                .OrderByDescending(candidate => (long)candidate.Width * candidate.Height)
                .ThenBy(candidate => candidate.CandidateId, StringComparer.Ordinal)
                .ToArray();
            return new(BookCoverSearchStatus.Succeeded, ordered);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(BookCoverSearchStatus.Failed, []);
        }
        catch (Exception exception) when (IsExpectedExternalFailure(exception, cancellationToken))
        {
            return new(BookCoverSearchStatus.Failed, [], exception.Message);
        }
    }

    public async Task<BookCoverDownloadResult> DownloadAsync(
        string candidateId,
        CancellationToken cancellationToken)
    {
        if (!long.TryParse(candidateId, NumberStyles.None, CultureInfo.InvariantCulture, out var coverId) || coverId <= 0)
        {
            return new(BookCoverDownloadStatus.InvalidCandidate);
        }

        try
        {
            var image = await TryDownloadImageAsync(
                coverId,
                "L",
                MaximumImageBytes,
                cancellationToken);
            return image is null
                ? new(BookCoverDownloadStatus.Failed)
                : new(
                    BookCoverDownloadStatus.Succeeded,
                    image.Value.Bytes,
                    image.Value.Width,
                    image.Value.Height);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(BookCoverDownloadStatus.Failed);
        }
        catch (Exception exception) when (IsExpectedExternalFailure(exception, cancellationToken))
        {
            return new(BookCoverDownloadStatus.Failed, Message: exception.Message);
        }
    }

    private static IEnumerable<Uri> CreateSearchUris(BookCoverSearchQuery query)
    {
        var title = query.Title?.Trim() ?? string.Empty;
        var author = query.Authors.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
        var common = "fields=cover_i,title,author_name,isbn&limit=" + SearchResultLimit.ToString(CultureInfo.InvariantCulture);

        if (IsbnValidator.TryNormalize(query.Isbn, out var isbn))
        {
            yield return new Uri(
                $"https://openlibrary.org/search.json?isbn={Uri.EscapeDataString(isbn!)}&{common}",
                UriKind.Absolute);
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            var authorPart = string.IsNullOrWhiteSpace(author)
                ? string.Empty
                : $"&author={Uri.EscapeDataString(author)}";
            yield return new Uri(
                $"https://openlibrary.org/search.json?title={Uri.EscapeDataString(title)}{authorPart}&{common}",
                UriKind.Absolute);
        }
    }

    private async Task<IReadOnlyList<SearchRow>> SearchRowsAsync(
        Uri requestUri,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        AddUserAgent(request);
        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var bytes = await ReadLimitedAsync(response.Content, MaximumJsonBytes, cancellationToken);
        using var document = JsonDocument.Parse(bytes);
        if (!document.RootElement.TryGetProperty("docs", out var docs) || docs.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var rows = new List<SearchRow>();
        foreach (var item in docs.EnumerateArray())
        {
            if (!item.TryGetProperty("cover_i", out var coverValue) ||
                coverValue.ValueKind != JsonValueKind.Number ||
                !coverValue.TryGetInt64(out var coverId) ||
                coverId <= 0)
            {
                continue;
            }

            var title = item.TryGetProperty("title", out var titleValue) && titleValue.ValueKind == JsonValueKind.String
                ? titleValue.GetString()?.Trim()
                : null;
            var authors = ReadStringArray(item, "author_name");
            var isbns = ReadStringArray(item, "isbn");
            rows.Add(new SearchRow(coverId, string.IsNullOrWhiteSpace(title) ? string.Empty : title, authors, isbns));
        }

        return rows;
    }

    private async Task<(byte[] Bytes, int Width, int Height)?> TryDownloadImageAsync(
        long coverId,
        string size,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        var uri = new Uri(
            $"https://covers.openlibrary.org/b/id/{coverId.ToString(CultureInfo.InvariantCulture)}-{size}.jpg?default=false",
            UriKind.Absolute);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/jpeg"));
        AddUserAgent(request);
        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (!response.IsSuccessStatusCode ||
            !string.Equals(response.Content.Headers.ContentType?.MediaType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var bytes = await ReadLimitedAsync(response.Content, maximumBytes, cancellationToken);
        if (!HasSafeDimensions(bytes) ||
            !imageValidator.TryValidateJpeg(bytes, out var width, out var height) ||
            !DimensionsAreSafe(width, height))
        {
            return null;
        }

        return (bytes, width, height);
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return values
            .EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.String)
            .Select(value => value.GetString()?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
    }

    private static bool HasSafeDimensions(byte[] bytes) =>
        JpegHeader.TryReadDimensions(bytes, out var width, out var height) && DimensionsAreSafe(width, height);

    private static bool DimensionsAreSafe(int width, int height) =>
        width >= MinimumDimension && height >= MinimumDimension &&
        width <= MaximumDimension && height <= MaximumDimension &&
        (long)width * height <= MaximumPixelCount;

    private static BookCoverCandidate ToMetadataCandidate(SearchRow row) =>
        new(
            Key,
            row.CoverId.ToString(CultureInfo.InvariantCulture),
            SourceName,
            row.Title,
            row.Authors,
            [],
            1,
            1,
            row.Isbns);

    private static async Task<byte[]> ReadLimitedAsync(
        HttpContent content,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is > 0 and var contentLength && contentLength > maximumBytes)
        {
            throw new InvalidDataException("The external response is too large.");
        }

        await using var input = await content.ReadAsStreamAsync(cancellationToken);
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var count = await input.ReadAsync(buffer, cancellationToken);
            if (count == 0)
            {
                break;
            }

            if (output.Length + count > maximumBytes)
            {
                throw new InvalidDataException("The external response is too large.");
            }

            output.Write(buffer, 0, count);
        }

        return output.ToArray();
    }

    private static bool IsExpectedExternalFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or InvalidDataException or JsonException ||
        exception is OperationCanceledException && !cancellationToken.IsCancellationRequested;

    private static void AddUserAgent(HttpRequestMessage request) =>
        request.Headers.UserAgent.ParseAdd("Saga/0.1 (+https://github.com/hnsoftwaredevelopment/saga)");

    private sealed record SearchRow(
        long CoverId,
        string Title,
        IReadOnlyList<string> Authors,
        IReadOnlyList<string> Isbns);
}

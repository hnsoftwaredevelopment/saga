using System.Net.Http.Headers;
using System.Xml;
using System.Xml.Linq;
using EbookManager.Application.Metadata;

namespace EbookManager.Infrastructure.Metadata;

public sealed class GoogleBooksBookCoverSource(
    HttpClient httpClient,
    IBookCoverImageValidator imageValidator) : IBookCoverSource
{
    public const string Key = "google-books";
    private const string SourceName = "Google Books";
    private const int MaximumCandidates = 12;
    private const int MaximumFeedBytes = 1024 * 1024;
    private const int MaximumPreviewBytes = 2 * 1024 * 1024;
    private const int MaximumImageBytes = 10 * 1024 * 1024;
    private const int MinimumDimension = 50;
    private const int MaximumDimension = 8_000;
    private const long MaximumPixelCount = 25_000_000;
    private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";
    private static readonly XNamespace DublinCore = "http://purl.org/dc/terms";
    private readonly HttpClient httpClient = httpClient;
    private readonly IBookCoverImageValidator imageValidator = imageValidator;

    public string SourceKey => Key;

    public async Task<BookCoverSearchResult> SearchAsync(
        BookCoverSearchQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var rows = new List<SearchRow>();
            var failed = false;
            foreach (var uri in CreateSearchUris(query))
            {
                try
                {
                    rows.AddRange(await ReadRowsAsync(uri, cancellationToken));
                }
                catch (Exception exception) when (IsExpectedFailure(exception, cancellationToken))
                {
                    failed = true;
                }
            }

            var uniqueRows = rows
                .Select(row => (Row: row, Score: BookCoverCandidateMatcher.Score(query, ToMetadataCandidate(row))))
                .Where(value => value.Score > 0)
                .OrderByDescending(value => value.Score)
                .Select(value => value.Row)
                .DistinctBy(row => row.Id)
                .Take(24)
                .ToArray();
            if (uniqueRows.Length == 0)
            {
                return new(failed ? BookCoverSearchStatus.Failed : BookCoverSearchStatus.NoResults, []);
            }

            var candidates = new List<BookCoverCandidate>(MaximumCandidates);
            foreach (var row in uniqueRows)
            {
                var preview = await TryDownloadImageAsync(row.Id, 5, MaximumPreviewBytes, cancellationToken);
                if (preview is null)
                {
                    continue;
                }

                candidates.Add(new(
                    Key,
                    row.Id,
                    SourceName,
                    row.Title,
                    row.Authors,
                    preview.Value.Bytes,
                    preview.Value.Width,
                    preview.Value.Height,
                    row.Isbns));
                if (candidates.Count == MaximumCandidates)
                {
                    break;
                }
            }

            return candidates.Count == 0
                ? new(BookCoverSearchStatus.NoResults, [])
                : new(BookCoverSearchStatus.Succeeded, candidates);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(BookCoverSearchStatus.Failed, []);
        }
        catch (Exception exception) when (IsExpectedFailure(exception, cancellationToken))
        {
            return new(BookCoverSearchStatus.Failed, [], exception.Message);
        }
    }

    public async Task<BookCoverDownloadResult> DownloadAsync(
        string candidateId,
        CancellationToken cancellationToken)
    {
        if (!IsValidBookId(candidateId))
        {
            return new(BookCoverDownloadStatus.InvalidCandidate);
        }

        try
        {
            var image = await TryDownloadImageAsync(candidateId, 0, MaximumImageBytes, cancellationToken);
            return image is null
                ? new(BookCoverDownloadStatus.Failed)
                : new(BookCoverDownloadStatus.Succeeded, image.Value.Bytes, image.Value.Width, image.Value.Height);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(BookCoverDownloadStatus.Failed);
        }
        catch (Exception exception) when (IsExpectedFailure(exception, cancellationToken))
        {
            return new(BookCoverDownloadStatus.Failed, Message: exception.Message);
        }
    }

    private static IEnumerable<Uri> CreateSearchUris(BookCoverSearchQuery query)
    {
        if (IsbnValidator.TryNormalize(query.Isbn, out var isbn))
        {
            yield return FeedUri("isbn:" + isbn);
        }

        var titleTerms = BookCoverCandidateMatcher.Tokens(query.Title)
            .Select(term => "intitle:" + term);
        var authorTerms = BookCoverCandidateMatcher.Tokens(
                query.Authors.FirstOrDefault(author => !string.IsNullOrWhiteSpace(author)))
            .Select(term => "inauthor:" + term);
        var fieldedQuery = string.Join(' ', titleTerms.Concat(authorTerms));
        if (!string.IsNullOrWhiteSpace(fieldedQuery))
        {
            yield return FeedUri(fieldedQuery);
        }
    }

    private static Uri FeedUri(string query) => new(
        $"https://books.google.com/books/feeds/volumes?q={Uri.EscapeDataString(query)}&max-results=24",
        UriKind.Absolute);

    private async Task<IReadOnlyList<SearchRow>> ReadRowsAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/atom+xml"));
        AddUserAgent(request);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var bytes = await ReadLimitedAsync(response.Content, MaximumFeedBytes, cancellationToken);
        using var input = new MemoryStream(bytes);
        using var reader = XmlReader.Create(input, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaximumFeedBytes
        });
        var document = XDocument.Load(reader, LoadOptions.None);

        return document.Descendants(Atom + "entry")
            .Select(ReadRow)
            .Where(row => row is not null)
            .Select(row => row!)
            .ToArray();
    }

    private static SearchRow? ReadRow(XElement entry)
    {
        var idText = entry.Element(Atom + "id")?.Value.Trim();
        var id = Uri.TryCreate(idText, UriKind.Absolute, out var idUri)
            ? idUri.Segments.LastOrDefault()?.Trim('/')
            : null;
        var hasThumbnail = entry.Elements(Atom + "link").Any(link =>
            link.Attribute("rel")?.Value.EndsWith("/thumbnail", StringComparison.Ordinal) == true);
        if (!hasThumbnail || !IsValidBookId(id))
        {
            return null;
        }

        var title = entry.Element(Atom + "title")?.Value.Trim() ?? string.Empty;
        var authors = entry.Elements(DublinCore + "creator")
            .Select(value => value.Value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        var isbns = entry.Elements(DublinCore + "identifier")
            .Select(value => value.Value.Trim())
            .Select(value => value.StartsWith("ISBN:", StringComparison.OrdinalIgnoreCase) ? value[5..].Trim() : value)
            .Where(value => IsbnValidator.TryNormalize(value, out _))
            .ToArray();
        return new(id!, title, authors, isbns);
    }

    private async Task<(byte[] Bytes, int Width, int Height)?> TryDownloadImageAsync(
        string id,
        int zoom,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        if (!IsValidBookId(id))
        {
            return null;
        }

        var uri = new Uri(
            $"https://books.google.com/books/content?id={Uri.EscapeDataString(id)}&printsec=frontcover&img=1&zoom={zoom}&source=gbs_gdata",
            UriKind.Absolute);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/jpeg"));
        AddUserAgent(request);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
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

    private static bool IsValidBookId(string? id) =>
        !string.IsNullOrWhiteSpace(id) && id.Length <= 64 &&
        id.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

    private static bool HasSafeDimensions(byte[] bytes) =>
        JpegHeader.TryReadDimensions(bytes, out var width, out var height) && DimensionsAreSafe(width, height);

    private static bool DimensionsAreSafe(int width, int height) =>
        width >= MinimumDimension && height >= MinimumDimension &&
        width <= MaximumDimension && height <= MaximumDimension &&
        (long)width * height <= MaximumPixelCount;

    private static BookCoverCandidate ToMetadataCandidate(SearchRow row) =>
        new(Key, row.Id, SourceName, row.Title, row.Authors, [], 1, 1, row.Isbns);

    private static async Task<byte[]> ReadLimitedAsync(HttpContent content, int maximumBytes, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is > 0 and var length && length > maximumBytes)
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
                return output.ToArray();
            }

            if (output.Length + count > maximumBytes)
            {
                throw new InvalidDataException("The external response is too large.");
            }

            output.Write(buffer, 0, count);
        }
    }

    private static bool IsExpectedFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or InvalidDataException or XmlException ||
        exception is OperationCanceledException && !cancellationToken.IsCancellationRequested;

    private static void AddUserAgent(HttpRequestMessage request) =>
        request.Headers.UserAgent.ParseAdd("Saga/0.1 (+https://github.com/hnsoftwaredevelopment/saga)");

    private sealed record SearchRow(
        string Id,
        string Title,
        IReadOnlyList<string> Authors,
        IReadOnlyList<string> Isbns);
}

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using EbookManager.Application.Metadata;
using EbookManager.Infrastructure.Metadata;
using FluentAssertions;

namespace EbookManager.Tests.Metadata;

public sealed class OpenLibraryBookCoverSearchServiceTests
{
    [Fact]
    public async Task Search_combines_isbn_and_title_author_results_and_returns_unique_covers_by_size()
    {
        var requests = new List<Uri>();
        using var client = new HttpClient(new StubHttpMessageHandler(request =>
        {
            requests.Add(request.RequestUri!);
            var query = request.RequestUri!.Query;
            if (request.RequestUri.Host == "openlibrary.org" && query.Contains("isbn=9789026356600", StringComparison.Ordinal))
            {
                return JsonResponse("""{"docs":[{"cover_i":20,"title":"Exact","author_name":["Auteur"]},{"cover_i":10,"title":"Dubbel","author_name":["Auteur"]}]}""");
            }

            if (request.RequestUri.Host == "openlibrary.org")
            {
                return JsonResponse("""{"docs":[{"cover_i":10,"title":"Titel","author_name":["Auteur"]},{"cover_i":30,"title":"Andere editie","author_name":["Auteur"]},{"cover_i":"ongeldig"}]}""");
            }

            var id = long.Parse(request.RequestUri.Segments[^1].Split('-')[0]);
            return JpegResponse(CreateJpeg(width: (int)id * 10, height: (int)id * 20));
        }));
        var service = new OpenLibraryBookCoverSearchService(client);

        var result = await service.SearchAsync(
            new BookCoverSearchQuery("De titel", ["De Auteur"], "9789026356600"),
            CancellationToken.None);

        result.Status.Should().Be(BookCoverSearchStatus.Succeeded);
        result.Candidates.Select(candidate => candidate.CoverId).Should().Equal(30, 20, 10);
        result.Candidates.Should().OnlyHaveUniqueItems(candidate => candidate.CoverId);
        result.Candidates[0].Source.Should().Be("Open Library");
        result.Candidates[0].PreviewBytes.Should().NotBeEmpty();
        requests.Should().Contain(uri => uri.Query.Contains("title=De%20titel", StringComparison.Ordinal));
        requests.Should().Contain(uri => uri.Query.Contains("author=De%20Auteur", StringComparison.Ordinal));
        requests.Should().Contain(uri => uri.Query.Contains("isbn=9789026356600", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Search_skips_invalid_images_and_reports_no_results()
    {
        using var client = new HttpClient(new StubHttpMessageHandler(request =>
            request.RequestUri!.Host == "openlibrary.org"
                ? JsonResponse("""{"docs":[{"cover_i":42,"title":"Titel"}]}""")
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent([1, 2, 3])
                    {
                        Headers = { ContentType = new MediaTypeHeaderValue("image/jpeg") }
                    }
                }));
        var service = new OpenLibraryBookCoverSearchService(client);

        var result = await service.SearchAsync(
            new BookCoverSearchQuery("Titel", ["Auteur"], null),
            CancellationToken.None);

        result.Status.Should().Be(BookCoverSearchStatus.NoResults);
        result.Candidates.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_rejects_a_response_larger_than_the_json_limit()
    {
        using var client = new HttpClient(new StubHttpMessageHandler(_ =>
        {
            var response = JsonResponse("{}");
            response.Content.Headers.ContentLength = 1024 * 1024 + 1;
            return response;
        }));
        var service = new OpenLibraryBookCoverSearchService(client);

        var result = await service.SearchAsync(
            new BookCoverSearchQuery("Titel", ["Auteur"], null),
            CancellationToken.None);

        result.Status.Should().Be(BookCoverSearchStatus.Failed);
        result.Candidates.Should().BeEmpty();
    }

    [Fact]
    public async Task Download_rejects_an_untrusted_candidate_identifier()
    {
        using var client = new HttpClient(new StubHttpMessageHandler(_ => throw new InvalidOperationException("HTTP should not be called.")));
        var service = new OpenLibraryBookCoverSearchService(client);

        var result = await service.DownloadAsync("https://localhost/private", CancellationToken.None);

        result.Status.Should().Be(BookCoverDownloadStatus.InvalidCandidate);
    }

    [Fact]
    public async Task Download_returns_validated_large_cover_bytes()
    {
        using var client = new HttpClient(new StubHttpMessageHandler(request =>
        {
            request.RequestUri!.AbsoluteUri.Should().Be("https://covers.openlibrary.org/b/id/123-L.jpg?default=false");
            return JpegResponse(CreateJpeg(600, 900));
        }));
        var service = new OpenLibraryBookCoverSearchService(client);

        var result = await service.DownloadAsync("123", CancellationToken.None);

        result.Status.Should().Be(BookCoverDownloadStatus.Succeeded);
        result.Width.Should().Be(600);
        result.Height.Should().Be(900);
        result.Bytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Search_identifies_saga_to_the_external_service()
    {
        string? userAgent = null;
        using var client = new HttpClient(new StubHttpMessageHandler(request =>
        {
            userAgent = request.Headers.UserAgent.ToString();
            return JsonResponse("""{"docs":[]}""");
        }));
        var service = new OpenLibraryBookCoverSearchService(client);

        await service.SearchAsync(
            new BookCoverSearchQuery("Titel", ["Auteur"], null),
            CancellationToken.None);

        userAgent.Should().Contain("Saga");
        userAgent.Should().Contain("github.com/hnsoftwaredevelopment/saga");
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private static HttpResponseMessage JpegResponse(byte[] bytes) => new(HttpStatusCode.OK)
    {
        Content = new ByteArrayContent(bytes)
        {
            Headers = { ContentType = new MediaTypeHeaderValue("image/jpeg") }
        }
    };

    private static byte[] CreateJpeg(int width, int height)
    {
        var bytes = new List<byte> { 0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08 };
        bytes.Add((byte)(height >> 8));
        bytes.Add((byte)height);
        bytes.Add((byte)(width >> 8));
        bytes.Add((byte)width);
        bytes.AddRange([0x03, 0x01, 0x11, 0x00, 0x02, 0x11, 0x00, 0x03, 0x11, 0x00, 0xFF, 0xD9]);
        return bytes.ToArray();
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}

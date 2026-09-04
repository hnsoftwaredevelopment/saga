using System.Net;
using System.Net.Http.Headers;
using System.Text;
using EbookManager.Application.Metadata;
using EbookManager.Infrastructure.Metadata;
using FluentAssertions;

namespace EbookManager.Tests.Metadata;

public sealed class GoogleBooksBookCoverSourceTests
{
    [Fact]
    public async Task Search_combines_isbn_and_metadata_and_returns_only_entries_with_a_cover()
    {
        var requests = new List<Uri>();
        using var client = new HttpClient(new StubHandler(request =>
        {
            requests.Add(request.RequestUri!);
            return request.RequestUri!.AbsolutePath.EndsWith("/feeds/volumes", StringComparison.Ordinal)
                ? AtomResponse(Feed(
                    Entry("valid_1", "De titel", "De Auteur", withThumbnail: true),
                    Entry("missing", "Zonder omslag", "Auteur", withThumbnail: false)))
                : JpegResponse(CreateJpeg(300, 450));
        }));
        var source = new GoogleBooksBookCoverSource(client);

        var result = await source.SearchAsync(
            new BookCoverSearchQuery("De titel", ["De Auteur"], "9789026356600"),
            CancellationToken.None);

        result.Status.Should().Be(BookCoverSearchStatus.Succeeded);
        result.Candidates.Should().ContainSingle();
        result.Candidates[0].Should().Match<BookCoverCandidate>(candidate =>
            candidate.SourceKey == GoogleBooksBookCoverSource.Key &&
            candidate.CandidateId == "valid_1" && candidate.Source == "Google Books");
        requests.Count(uri => uri.AbsolutePath.EndsWith("/feeds/volumes", StringComparison.Ordinal)).Should().Be(2);
        requests.Should().Contain(uri => Uri.UnescapeDataString(uri.Query).Contains("isbn:9789026356600", StringComparison.Ordinal));
        requests.Should().Contain(uri => Uri.UnescapeDataString(uri.Query).Contains("De titel De Auteur", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Search_rejects_an_external_identifier_and_does_not_download_it()
    {
        var imageRequested = false;
        using var client = new HttpClient(new StubHandler(request =>
        {
            if (!request.RequestUri!.AbsolutePath.EndsWith("/feeds/volumes", StringComparison.Ordinal))
            {
                imageRequested = true;
            }

            return AtomResponse(Feed(Entry("..%2Fprivate", "Titel", "Auteur", withThumbnail: true)));
        }));
        var source = new GoogleBooksBookCoverSource(client);

        var result = await source.SearchAsync(new("Titel", ["Auteur"], null), CancellationToken.None);

        result.Status.Should().Be(BookCoverSearchStatus.NoResults);
        imageRequested.Should().BeFalse();
    }

    [Fact]
    public async Task Download_builds_a_fixed_google_books_uri_and_validates_the_jpeg()
    {
        Uri? requested = null;
        using var client = new HttpClient(new StubHandler(request =>
        {
            requested = request.RequestUri;
            return JpegResponse(CreateJpeg(600, 900));
        }));
        var source = new GoogleBooksBookCoverSource(client);

        var result = await source.DownloadAsync("AbC_123-x", CancellationToken.None);

        result.Status.Should().Be(BookCoverDownloadStatus.Succeeded);
        result.Width.Should().Be(600);
        requested!.Host.Should().Be("books.google.com");
        requested.AbsolutePath.Should().Be("/books/content");
        Uri.UnescapeDataString(requested.Query).Should().Contain("id=AbC_123-x").And.Contain("zoom=0");
    }

    [Theory]
    [InlineData("https://localhost/private")]
    [InlineData("../private")]
    [InlineData("")]
    public async Task Download_rejects_untrusted_identifiers(string identifier)
    {
        using var client = new HttpClient(new StubHandler(_ => throw new InvalidOperationException()));
        var result = await new GoogleBooksBookCoverSource(client).DownloadAsync(identifier, CancellationToken.None);
        result.Status.Should().Be(BookCoverDownloadStatus.InvalidCandidate);
    }

    private static string Feed(params string[] entries) => $$"""
        <?xml version="1.0" encoding="utf-8"?>
        <feed xmlns="http://www.w3.org/2005/Atom" xmlns:dc="http://purl.org/dc/terms">
          {{string.Join(Environment.NewLine, entries)}}
        </feed>
        """;

    private static string Entry(string id, string title, string author, bool withThumbnail) => $$"""
        <entry>
          <id>http://www.google.com/books/feeds/volumes/{{id}}</id>
          <title>{{title}}</title>
          <dc:creator>{{author}}</dc:creator>
          {{(withThumbnail ? "<link rel=\"http://schemas.google.com/books/2008/thumbnail\" href=\"http://books.google.com/cover\" />" : string.Empty)}}
        </entry>
        """;

    private static HttpResponseMessage AtomResponse(string value) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(value, Encoding.UTF8, "application/atom+xml")
    };

    private static HttpResponseMessage JpegResponse(byte[] bytes) => new(HttpStatusCode.OK)
    {
        Content = new ByteArrayContent(bytes) { Headers = { ContentType = new MediaTypeHeaderValue("image/jpeg") } }
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

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}

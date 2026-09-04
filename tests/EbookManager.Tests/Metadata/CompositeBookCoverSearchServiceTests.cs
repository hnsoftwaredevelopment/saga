using EbookManager.Application.Metadata;
using FluentAssertions;

namespace EbookManager.Tests.Metadata;

public sealed class CompositeBookCoverSearchServiceTests
{
    [Fact]
    public async Task Search_interleaves_sources_and_limits_the_combined_result()
    {
        var first = new StubSource("first", Candidates("first", 10));
        var second = new StubSource("second", Candidates("second", 10));
        var service = new CompositeBookCoverSearchService([first, second]);

        var result = await service.SearchAsync(Query(), CancellationToken.None);

        result.Status.Should().Be(BookCoverSearchStatus.Succeeded);
        result.Candidates.Should().HaveCount(12);
        result.Candidates.Take(4).Select(value => value.SourceKey)
            .Should().Equal("first", "second", "first", "second");
    }

    [Fact]
    public async Task Search_uses_fallback_only_when_all_online_sources_are_empty()
    {
        var fallback = new StubSource("generated", Candidates("generated", 1));
        var service = new CompositeBookCoverSearchService(
            [new StubSource("first", []), new StubSource("second", [])],
            fallback);

        var result = await service.SearchAsync(Query(), CancellationToken.None);

        result.Candidates.Should().ContainSingle().Which.SourceKey.Should().Be("generated");
        fallback.SearchCount.Should().Be(1);
    }

    [Fact]
    public async Task Search_keeps_successful_source_when_another_source_fails()
    {
        var expected = Candidates("working", 1)[0];
        var service = new CompositeBookCoverSearchService([
            new StubSource("broken", [], throwOnSearch: true),
            new StubSource("working", [expected])]);

        var result = await service.SearchAsync(Query(), CancellationToken.None);

        result.Status.Should().Be(BookCoverSearchStatus.Succeeded);
        result.Candidates.Should().Equal(expected);
    }

    [Fact]
    public async Task Download_routes_only_to_the_matching_registered_source()
    {
        var first = new StubSource("first", []);
        var second = new StubSource("second", []);
        var service = new CompositeBookCoverSearchService([first, second]);
        var candidate = Candidate("second", "7");

        var result = await service.DownloadAsync(candidate, CancellationToken.None);
        var invalid = await service.DownloadAsync(candidate with { SourceKey = "unknown" }, CancellationToken.None);

        result.Status.Should().Be(BookCoverDownloadStatus.Succeeded);
        second.DownloadedId.Should().Be("7");
        first.DownloadedId.Should().BeNull();
        invalid.Status.Should().Be(BookCoverDownloadStatus.InvalidCandidate);
    }

    private static BookCoverSearchQuery Query() => new("Titel", ["Auteur"], null);

    private static IReadOnlyList<BookCoverCandidate> Candidates(string source, int count) =>
        Enumerable.Range(1, count).Select(index => Candidate(source, index.ToString())).ToArray();

    private static BookCoverCandidate Candidate(string source, string id) =>
        new(source, id, source, "Titel", ["Auteur"], [0xFF, 0xD8, 0xFF, 0xD9], 100, 150);

    private sealed class StubSource(
        string sourceKey,
        IReadOnlyList<BookCoverCandidate> candidates,
        bool throwOnSearch = false) : IBookCoverSource
    {
        public string SourceKey => sourceKey;
        public int SearchCount { get; private set; }
        public string? DownloadedId { get; private set; }

        public Task<BookCoverSearchResult> SearchAsync(BookCoverSearchQuery query, CancellationToken cancellationToken)
        {
            SearchCount++;
            if (throwOnSearch)
            {
                throw new HttpRequestException("Unavailable");
            }

            return Task.FromResult(new BookCoverSearchResult(
                candidates.Count == 0 ? BookCoverSearchStatus.NoResults : BookCoverSearchStatus.Succeeded,
                candidates));
        }

        public Task<BookCoverDownloadResult> DownloadAsync(string candidateId, CancellationToken cancellationToken)
        {
            DownloadedId = candidateId;
            return Task.FromResult(new BookCoverDownloadResult(BookCoverDownloadStatus.Succeeded, [1], 1, 1));
        }
    }
}

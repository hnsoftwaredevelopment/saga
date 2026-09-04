using EbookManager.Application.Metadata;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class MetadataQualityCoverSearchViewModelTests
{
    [Fact]
    public async Task LoadAsync_Shows_Results_Without_Choosing_For_The_User()
    {
        var first = Candidate(11, 400, 600);
        var second = Candidate(12, 300, 500);
        var service = new StubCoverSearchService(new(
            BookCoverSearchStatus.Succeeded,
            [first, second]));
        var viewModel = new MetadataQualityCoverSearchViewModel(
            new("The book", ["The author"], "9781234567890"),
            service,
            key => key);

        await viewModel.LoadAsync(CancellationToken.None);

        viewModel.Candidates.Should().Equal(first, second);
        viewModel.SelectedCandidate.Should().BeNull();
        viewModel.CanUseCover.Should().BeFalse();
        viewModel.IsLoading.Should().BeFalse();
        viewModel.StatusMessage.Should().BeNull();

        viewModel.SelectedCandidate = second;
        viewModel.CanUseCover.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_Shows_Empty_Message_When_No_Covers_Are_Found()
    {
        var service = new StubCoverSearchService(new(
            BookCoverSearchStatus.NoResults,
            []));
        var viewModel = new MetadataQualityCoverSearchViewModel(
            new("Unknown", [], null),
            service,
            key => $"loc:{key}");

        await viewModel.LoadAsync(CancellationToken.None);

        viewModel.Candidates.Should().BeEmpty();
        viewModel.SelectedCandidate.Should().BeNull();
        viewModel.CanUseCover.Should().BeFalse();
        viewModel.StatusMessage.Should().Be("loc:MetadataQualityCoverSearchNoResults");
    }

    [Fact]
    public async Task LoadAsync_Shows_Friendly_Error_When_Search_Fails()
    {
        var service = new StubCoverSearchService(new(
            BookCoverSearchStatus.Failed,
            [],
            "technical details"));
        var viewModel = new MetadataQualityCoverSearchViewModel(
            new("Unknown", [], null),
            service,
            key => $"loc:{key}");

        await viewModel.LoadAsync(CancellationToken.None);

        viewModel.StatusMessage.Should().Be("loc:MetadataQualityCoverSearchFailed");
        viewModel.CanUseCover.Should().BeFalse();
    }

    private static BookCoverCandidate Candidate(long id, int width, int height) =>
        new(id, "Open Library", $"Title {id}", ["Author"], [0xFF, 0xD8, 0xFF, 0xD9], width, height);

    private sealed class StubCoverSearchService(BookCoverSearchResult result) : IBookCoverSearchService
    {
        public Task<BookCoverSearchResult> SearchAsync(
            BookCoverSearchQuery query,
            CancellationToken cancellationToken) => Task.FromResult(result);

        public Task<BookCoverDownloadResult> DownloadAsync(
            string candidateId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

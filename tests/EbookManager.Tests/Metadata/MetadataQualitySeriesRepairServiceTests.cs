using EbookManager.Application.Books;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using FluentAssertions;

namespace EbookManager.Tests.Metadata;

public sealed class MetadataQualitySeriesRepairServiceTests
{
    [Fact]
    public async Task RepairAsync_changes_only_series_and_preserves_the_existing_series_number()
    {
        var original = CreateBook(series: null, seriesNumber: 3);
        var repository = new InMemoryBookRepository([original]);
        var service = CreateService(repository);

        var result = await service.RepairAsync([original.Id], "  De Donkere Toren  ", default);

        var repaired = result.Items.Should().ContainSingle().Which;
        repaired.Status.Should().Be(MetadataQualitySeriesRepairStatus.Succeeded);
        repaired.Book!.Metadata.Series.Should().Be("De Donkere Toren");
        repaired.Book.Metadata.SeriesNumber.Should().Be(3);
        repaired.Book.Metadata.Title.Should().Be(original.Metadata.Title);
        repaired.Book.Metadata.Authors.Should().Equal(original.Metadata.Authors);
        repaired.Book.Metadata.Language.Should().Be(original.Metadata.Language);
        repaired.Book.Metadata.Tags.Should().Equal(original.Metadata.Tags!);
        repaired.Book.UpdatedUtc.Should().BeAfter(original.UpdatedUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RepairAsync_rejects_an_empty_series_without_writing(string series)
    {
        var original = CreateBook(series: null, seriesNumber: 1);
        var repository = new InMemoryBookRepository([original]);
        var service = CreateService(repository);

        var action = () => service.RepairAsync([original.Id], series, default);

        await action.Should().ThrowAsync<ArgumentException>();
        repository.UpdateCalls.Should().Be(0);
    }

    [Fact]
    public async Task RepairAsync_does_not_overwrite_a_book_that_no_longer_has_the_quality_issue()
    {
        var current = CreateBook(series: "Bestaande serie", seriesNumber: 2);
        var repository = new InMemoryBookRepository([current]);
        var service = CreateService(repository);

        var result = await service.RepairAsync([current.Id], "Andere serie", default);

        result.Items.Should().ContainSingle().Which.Status
            .Should().Be(MetadataQualitySeriesRepairStatus.NotApplicable);
        repository.UpdateCalls.Should().Be(0);
        (await repository.GetAsync(current.Id, default))!.Metadata.Series.Should().Be("Bestaande serie");
    }

    [Fact]
    public async Task RepairAsync_is_not_applicable_when_the_series_number_is_missing()
    {
        var current = CreateBook(series: null, seriesNumber: null);
        var repository = new InMemoryBookRepository([current]);
        var service = CreateService(repository);

        var result = await service.RepairAsync([current.Id], "Nieuwe serie", default);

        result.Items.Should().ContainSingle().Which.Status
            .Should().Be(MetadataQualitySeriesRepairStatus.NotApplicable);
        repository.UpdateCalls.Should().Be(0);
    }

    [Fact]
    public async Task RepairAsync_reports_saved_with_writeback_errors_when_database_update_succeeded()
    {
        var original = CreateBook(series: null, seriesNumber: 4);
        var repository = new InMemoryBookRepository([original]) { ThrowWhenListingFiles = true };
        var service = CreateService(repository);

        var result = await service.RepairAsync([original.Id], "Saga", default);

        var repaired = result.Items.Should().ContainSingle().Which;
        repaired.Status.Should().Be(MetadataQualitySeriesRepairStatus.SavedWithWriteBackErrors);
        repaired.Book!.Metadata.Series.Should().Be("Saga");
    }

    private static MetadataQualitySeriesRepairService CreateService(InMemoryBookRepository repository) =>
        new(
            repository,
            new BookService(repository, new NoopLibraryFileStore(), new ThrowingMetadataAdapterResolver()));

    private static Book CreateBook(string? series, decimal? seriesNumber)
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata(
                "Boek",
                ["Auteur"],
                Description: "Beschrijving",
                Language: "nl",
                Tags: ["Tag"],
                Series: series,
                SeriesNumber: seriesNumber),
            ReadingStatus.Unread,
            null,
            now.AddDays(-2),
            now.AddDays(-1));
    }

    private sealed class InMemoryBookRepository(IEnumerable<Book> seed) : IBookRepository
    {
        private readonly Dictionary<Guid, Book> books = seed.ToDictionary(book => book.Id);

        public int UpdateCalls { get; private set; }
        public bool ThrowWhenListingFiles { get; init; }

        public Task<IReadOnlyList<Book>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Book>>(books.Values.ToArray());
        public Task<Book?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(books.GetValueOrDefault(id));
        public Task UpdateAsync(Book book, CancellationToken cancellationToken)
        {
            UpdateCalls++;
            books[book.Id] = book;
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<BookFile>> ListFilesAsync(Guid bookId, CancellationToken cancellationToken) =>
            ThrowWhenListingFiles
                ? throw new IOException("Write-back unavailable.")
                : Task.FromResult<IReadOnlyList<BookFile>>([]);
        public Task<bool> HasHashAsync(string sha256, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> HasNormalizedTitleAndAuthorAsync(string title, IReadOnlyList<string> authors, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<Book?> FindByNormalizedTitleAndAuthorAsync(string title, IReadOnlyList<string> authors, CancellationToken cancellationToken) => Task.FromResult<Book?>(null);
        public Task<IReadOnlyList<Book>> FindByNormalizedTitleAsync(string title, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Book>>([]);
        public Task AddAsync(Book book, BookFile file, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddFileAsync(BookFile file, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AttachFilesToBookAsync(Guid sourceBookId, Guid targetBookId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<BookFileDeleteRepositoryResult> DeleteFileAsync(Guid bookId, Guid fileId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task UpdateFileWriteBackAsync(Guid fileId, MetadataWriteResult result, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class NoopLibraryFileStore : ILibraryFileStore
    {
        public string GetAbsolutePath(string relativePath) => relativePath;
        public Task<(string RelativeBookPath, string? RelativeCoverPath)> CopyIntoLibraryAsync(Guid bookId, string sourcePath, byte[]? coverBytes, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteBookDirectoryAsync(Guid bookId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ThrowingMetadataAdapterResolver : IMetadataAdapterResolver
    {
        public IMetadataAdapter Resolve(EbookFormat format) => throw new InvalidOperationException("No files expected.");
    }
}

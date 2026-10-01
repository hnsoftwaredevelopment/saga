using EbookManager.Application.Books;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using FluentAssertions;

namespace EbookManager.Tests.Metadata;

public sealed class MetadataQualityTagRepairServiceTests
{
    [Fact]
    public void Normalize_splits_values_and_preserves_the_first_unique_spelling_and_order()
    {
        var normalized = MetadataQualityTagNormalizer.Normalize(
            [" Thriller ", "Misdaad,  Spanning", "thriller", "Science   Fiction", " "]);

        normalized.Should().Equal("Thriller", "Misdaad", "Spanning", "Science Fiction");
    }

    [Fact]
    public void Normalize_supports_multiline_editor_text_and_an_empty_result()
    {
        MetadataQualityTagNormalizer.Normalize(["Eerste\r\nTweede, Derde"])
            .Should().Equal("Eerste", "Tweede", "Derde");
        MetadataQualityTagNormalizer.Normalize([" \r\n , "]).Should().BeEmpty();
    }

    [Fact]
    public async Task RepairAsync_changes_only_tags_and_updated_time()
    {
        var original = CreateBook([" Thriller ", "Misdaad, Spanning", "thriller"]);
        var repository = new InMemoryBookRepository([original]);
        var service = CreateService(repository);

        var result = await service.RepairAsync(
            original.Id,
            ["Thriller", "Misdaad", "Spanning"],
            default);

        result.Status.Should().Be(MetadataQualityTagRepairStatus.Succeeded);
        result.Book!.Metadata.Tags.Should().Equal("Thriller", "Misdaad", "Spanning");
        result.Book.Metadata.Title.Should().Be(original.Metadata.Title);
        result.Book.Metadata.Authors.Should().Equal(original.Metadata.Authors);
        result.Book.Metadata.Description.Should().Be(original.Metadata.Description);
        result.Book.Metadata.Language.Should().Be(original.Metadata.Language);
        result.Book.Metadata.Publisher.Should().Be(original.Metadata.Publisher);
        result.Book.Metadata.Series.Should().Be(original.Metadata.Series);
        result.Book.Metadata.SeriesNumber.Should().Be(original.Metadata.SeriesNumber);
        result.Book.Metadata.Isbn.Should().Be(original.Metadata.Isbn);
        result.Book.Metadata.CoverBytes.Should().Equal(original.Metadata.CoverBytes!);
        result.Book.UpdatedUtc.Should().BeAfter(original.UpdatedUtc);
    }

    [Fact]
    public async Task RepairAsync_allows_removing_every_tag()
    {
        var original = CreateBook([" , "]);
        var repository = new InMemoryBookRepository([original]);

        var result = await CreateService(repository).RepairAsync(original.Id, [], default);

        result.Status.Should().Be(MetadataQualityTagRepairStatus.Succeeded);
        result.Book!.Metadata.Tags.Should().BeNull();
    }

    [Fact]
    public async Task RepairAsync_does_not_overwrite_a_book_that_no_longer_has_messy_tags()
    {
        var current = CreateBook(["Thriller", "Spanning"]);
        var repository = new InMemoryBookRepository([current]);

        var result = await CreateService(repository).RepairAsync(current.Id, ["Anders"], default);

        result.Status.Should().Be(MetadataQualityTagRepairStatus.NotApplicable);
        repository.UpdateCalls.Should().Be(0);
        (await repository.GetAsync(current.Id, default))!.Metadata.Tags.Should().Equal("Thriller", "Spanning");
    }

    [Fact]
    public async Task RepairAsync_reports_not_found_without_writing()
    {
        var repository = new InMemoryBookRepository([]);

        var result = await CreateService(repository).RepairAsync(Guid.NewGuid(), ["Tag"], default);

        result.Status.Should().Be(MetadataQualityTagRepairStatus.NotFound);
        repository.UpdateCalls.Should().Be(0);
    }

    [Fact]
    public async Task RepairAsync_reports_saved_with_writeback_errors_when_database_update_succeeded()
    {
        var original = CreateBook(["Tag, Tweede"]);
        var repository = new InMemoryBookRepository([original]) { ThrowWhenListingFiles = true };

        var result = await CreateService(repository).RepairAsync(original.Id, ["Tag", "Tweede"], default);

        result.Status.Should().Be(MetadataQualityTagRepairStatus.SavedWithWriteBackErrors);
        result.Book!.Metadata.Tags.Should().Equal("Tag", "Tweede");
    }

    [Fact]
    public async Task RepairAsync_reports_failure_when_the_database_was_not_updated()
    {
        var original = CreateBook(["Tag, Tweede"]);
        var repository = new InMemoryBookRepository([original]) { ThrowWhenUpdating = true };

        var result = await CreateService(repository).RepairAsync(original.Id, ["Tag", "Tweede"], default);

        result.Status.Should().Be(MetadataQualityTagRepairStatus.Failed);
        result.Book!.Metadata.Tags.Should().Equal("Tag, Tweede");
    }

    private static MetadataQualityTagRepairService CreateService(InMemoryBookRepository repository) =>
        new(
            repository,
            new BookService(repository, new NoopLibraryFileStore(), new ThrowingMetadataAdapterResolver()));

    private static Book CreateBook(IReadOnlyList<string>? tags)
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata(
                "Boek",
                ["Auteur"],
                Description: "Beschrijving",
                Language: "nl",
                Publisher: "Uitgever",
                PublicationDate: new DateOnly(2020, 1, 2),
                Tags: tags,
                Series: "Serie",
                SeriesNumber: 2,
                Isbn: "9789020000000",
                CoverBytes: [1, 2, 3]),
            ReadingStatus.Reading,
            "cover.jpg",
            now.AddDays(-2),
            now.AddDays(-1));
    }

    private sealed class InMemoryBookRepository(IEnumerable<Book> seed) : IBookRepository
    {
        private readonly Dictionary<Guid, Book> books = seed.ToDictionary(book => book.Id);

        public int UpdateCalls { get; private set; }
        public bool ThrowWhenListingFiles { get; init; }
        public bool ThrowWhenUpdating { get; init; }

        public Task<IReadOnlyList<Book>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Book>>(books.Values.ToArray());
        public Task<Book?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(books.GetValueOrDefault(id));
        public Task UpdateAsync(Book book, CancellationToken cancellationToken)
        {
            UpdateCalls++;
            if (ThrowWhenUpdating)
            {
                throw new IOException("Database unavailable.");
            }

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

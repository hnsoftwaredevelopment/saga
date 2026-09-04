using EbookManager.Application.Books;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using FluentAssertions;

namespace EbookManager.Tests.Metadata;

public sealed class MetadataQualityTitleAuthorRepairServiceTests
{
    [Fact]
    public async Task RepairAsync_swaps_only_title_and_the_single_author()
    {
        var original = CreateBook("Jan Jansen", ["De verdwenen stad"]);
        var repository = new InMemoryBookRepository([original]);
        var service = CreateService(repository);

        var result = await service.RepairAsync(original.Id, default);

        var repaired = result;
        repaired.Status.Should().Be(MetadataQualityTitleAuthorRepairStatus.Succeeded);
        repaired.Book!.Metadata.Title.Should().Be("De verdwenen stad");
        repaired.Book.Metadata.Authors.Should().Equal("Jan Jansen");
        repaired.Book.Metadata.Description.Should().Be(original.Metadata.Description);
        repaired.Book.Metadata.Language.Should().Be(original.Metadata.Language);
        repaired.Book.Metadata.Publisher.Should().Be(original.Metadata.Publisher);
        repaired.Book.Metadata.PublicationDate.Should().Be(original.Metadata.PublicationDate);
        repaired.Book.Metadata.Tags.Should().Equal(original.Metadata.Tags!);
        repaired.Book.Metadata.Series.Should().Be(original.Metadata.Series);
        repaired.Book.Metadata.SeriesNumber.Should().Be(original.Metadata.SeriesNumber);
        repaired.Book.Metadata.Isbn.Should().Be(original.Metadata.Isbn);
        repaired.Book.Metadata.CoverBytes.Should().Equal(original.Metadata.CoverBytes!);
        repaired.Book.ReadingStatus.Should().Be(original.ReadingStatus);
        repaired.Book.Formats.Should().Equal(original.Formats);
        repaired.Book.UpdatedUtc.Should().BeAfter(original.UpdatedUtc);
        repository.GetCalls.Should().Be(2);
    }

    [Fact]
    public async Task RepairAsync_does_not_change_a_book_when_the_signal_no_longer_applies()
    {
        var current = CreateBook("Dune", ["Frank Herbert"]);
        var repository = new InMemoryBookRepository([current]);
        var service = CreateService(repository);

        var result = await service.RepairAsync(current.Id, default);

        result.Status.Should().Be(MetadataQualityTitleAuthorRepairStatus.NotApplicable);
        repository.UpdateCalls.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Unknown")]
    public async Task RepairAsync_rejects_an_unusable_author_even_when_the_signal_applies(string author)
    {
        var current = CreateBook("Jan Jansen", [author]);
        var repository = new InMemoryBookRepository([current]);
        var service = CreateService(repository);

        var result = await service.RepairAsync(current.Id, default);

        result.Status.Should().Be(MetadataQualityTitleAuthorRepairStatus.NotApplicable);
        repository.UpdateCalls.Should().Be(0);
    }

    [Fact]
    public async Task RepairAsync_reports_a_missing_book()
    {
        var repository = new InMemoryBookRepository([]);
        var service = CreateService(repository);

        var result = await service.RepairAsync(Guid.NewGuid(), default);

        result.Status.Should().Be(MetadataQualityTitleAuthorRepairStatus.NotFound);
    }

    [Fact]
    public async Task RepairAsync_reports_a_save_conflict_without_changing_the_book()
    {
        var original = CreateBook("Jan Jansen", ["De verdwenen stad"]);
        var repository = new InMemoryBookRepository([original]) { ThrowConflict = true };
        var service = CreateService(repository);

        var result = await service.RepairAsync(original.Id, default);

        result.Status.Should().Be(MetadataQualityTitleAuthorRepairStatus.Failed);
        (await repository.GetAsync(original.Id, default))!.Metadata.Should().Be(original.Metadata);
    }

    [Fact]
    public async Task RepairAsync_reports_writeback_warning_after_database_success()
    {
        var original = CreateBook("Jan Jansen", ["De verdwenen stad"]);
        var repository = new InMemoryBookRepository([original]) { ThrowWhenListingFiles = true };
        var service = CreateService(repository);

        var result = await service.RepairAsync(original.Id, default);

        var repaired = result;
        repaired.Status.Should().Be(MetadataQualityTitleAuthorRepairStatus.SavedWithWriteBackErrors);
        repaired.Book!.Metadata.Title.Should().Be("De verdwenen stad");
        repaired.Book.Metadata.Authors.Should().Equal("Jan Jansen");
    }

    private static MetadataQualityTitleAuthorRepairService CreateService(InMemoryBookRepository repository) =>
        new(
            repository,
            new BookService(repository, new NoopLibraryFileStore(), new ThrowingMetadataAdapterResolver()));

    private static Book CreateBook(string title, IReadOnlyList<string> authors)
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata(
                title,
                authors,
                Description: "Beschrijving",
                Language: "nl",
                Publisher: "Uitgever",
                PublicationDate: new DateOnly(2024, 3, 2),
                Tags: ["Tag"],
                Series: "Serie",
                SeriesNumber: 2,
                Isbn: "9780000000001",
                CoverBytes: [1, 2, 3]),
            ReadingStatus.Reading,
            "covers/cover.jpg",
            now.AddDays(-2),
            now.AddDays(-1))
        {
            Formats = [EbookFormat.Epub]
        };
    }

    private sealed class InMemoryBookRepository(IEnumerable<Book> seed) : IBookRepository
    {
        private readonly Dictionary<Guid, Book> books = seed.ToDictionary(book => book.Id);

        public int GetCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public bool ThrowConflict { get; init; }
        public bool ThrowWhenListingFiles { get; init; }

        public Task<IReadOnlyList<Book>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Book>>(books.Values.ToArray());

        public Task<Book?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            GetCalls++;
            return Task.FromResult(books.GetValueOrDefault(id));
        }

        public Task UpdateAsync(Book book, CancellationToken cancellationToken)
        {
            UpdateCalls++;
            if (ThrowConflict)
            {
                throw new BookConflictException();
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

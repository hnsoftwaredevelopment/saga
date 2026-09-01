using EbookManager.Application.Books;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using FluentAssertions;

namespace EbookManager.Tests.Metadata;

public sealed class MetadataQualityAuthorRepairServiceTests
{
    [Fact]
    public async Task RepairAsync_changes_only_author_and_updated_timestamp_then_returns_reloaded_book()
    {
        var original = CreateBook("Boek", ["Unknown"]);
        var repository = new InMemoryBookRepository([original]);
        var service = CreateService(repository);

        var result = await service.RepairAsync([original.Id], "  Nieuwe Auteur  ", default);

        var repaired = result.Items.Should().ContainSingle().Which;
        repaired.Status.Should().Be(MetadataQualityAuthorRepairStatus.Succeeded);
        repaired.Book.Should().NotBeNull();
        repaired.Book!.Metadata.Authors.Should().Equal("Nieuwe Auteur");
        repaired.Book.Metadata.Title.Should().Be(original.Metadata.Title);
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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Unknown")]
    [InlineData("unknown")]
    public async Task RepairAsync_rejects_invalid_author_without_writing(string author)
    {
        var original = CreateBook("Boek", ["Unknown"]);
        var repository = new InMemoryBookRepository([original]);
        var service = CreateService(repository);

        var action = () => service.RepairAsync([original.Id], author, default);

        await action.Should().ThrowAsync<ArgumentException>();
        repository.UpdateCalls.Should().Be(0);
    }

    [Fact]
    public async Task RepairAsync_rejects_empty_book_selection_without_writing()
    {
        var repository = new InMemoryBookRepository([]);
        var service = CreateService(repository);

        var action = () => service.RepairAsync([], "Auteur", default);

        await action.Should().ThrowAsync<ArgumentException>();
        repository.UpdateCalls.Should().Be(0);
    }

    [Fact]
    public async Task RepairAsync_reports_each_book_when_one_is_missing()
    {
        var existing = CreateBook("Aanwezig", ["Unknown"]);
        var missingId = Guid.NewGuid();
        var repository = new InMemoryBookRepository([existing]);
        var service = CreateService(repository);

        var result = await service.RepairAsync([existing.Id, missingId], "Auteur", default);

        result.Items.Should().HaveCount(2);
        result.Items.Single(item => item.BookId == existing.Id).Status
            .Should().Be(MetadataQualityAuthorRepairStatus.Succeeded);
        result.Items.Single(item => item.BookId == missingId).Status
            .Should().Be(MetadataQualityAuthorRepairStatus.NotFound);
    }

    [Fact]
    public async Task RepairAsync_does_not_overwrite_a_book_that_already_has_a_valid_author()
    {
        var existing = CreateBook("Boek", ["Bestaande Auteur"]);
        var repository = new InMemoryBookRepository([existing]);
        var service = CreateService(repository);

        var result = await service.RepairAsync([existing.Id], "Andere Auteur", default);

        result.Items.Should().ContainSingle().Which.Status
            .Should().Be(MetadataQualityAuthorRepairStatus.NotApplicable);
        repository.UpdateCalls.Should().Be(0);
        (await repository.GetAsync(existing.Id, default))!.Metadata.Authors
            .Should().Equal("Bestaande Auteur");
    }

    [Fact]
    public async Task RepairAsync_reports_a_conflict_and_continues_with_the_next_book()
    {
        var conflicting = CreateBook("Conflict", ["Unknown"]);
        var successful = CreateBook("Succes", ["Unknown"]);
        var repository = new InMemoryBookRepository([conflicting, successful]);
        repository.ConflictingBookIds.Add(conflicting.Id);
        var service = CreateService(repository);

        var result = await service.RepairAsync([conflicting.Id, successful.Id], "Auteur", default);

        result.Items.Single(item => item.BookId == conflicting.Id).Status
            .Should().Be(MetadataQualityAuthorRepairStatus.Failed);
        result.Items.Single(item => item.BookId == successful.Id).Status
            .Should().Be(MetadataQualityAuthorRepairStatus.Succeeded);
    }

    [Fact]
    public async Task RepairAsync_reports_saved_with_writeback_errors_when_database_update_succeeded()
    {
        var original = CreateBook("Boek", ["Unknown"]);
        var repository = new InMemoryBookRepository([original])
        {
            ThrowWhenListingFiles = true
        };
        var service = CreateService(repository);

        var result = await service.RepairAsync([original.Id], "Nieuwe Auteur", default);

        var repaired = result.Items.Should().ContainSingle().Which;
        repaired.Status.Should().Be(MetadataQualityAuthorRepairStatus.SavedWithWriteBackErrors);
        repaired.Book!.Metadata.Authors.Should().Equal("Nieuwe Auteur");
    }

    private static MetadataQualityAuthorRepairService CreateService(InMemoryBookRepository repository)
    {
        var bookService = new BookService(
            repository,
            new NoopLibraryFileStore(),
            new ThrowingMetadataAdapterResolver());
        return new MetadataQualityAuthorRepairService(repository, bookService);
    }

    private static Book CreateBook(string title, IReadOnlyList<string> authors)
    {
        var created = DateTimeOffset.UtcNow.AddDays(-2);
        var updated = DateTimeOffset.UtcNow.AddDays(-1);
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
            created,
            updated)
        {
            Formats = [EbookFormat.Epub]
        };
    }

    private sealed class InMemoryBookRepository(IEnumerable<Book> seed) : IBookRepository
    {
        private readonly Dictionary<Guid, Book> books = seed.ToDictionary(book => book.Id);

        public int GetCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public HashSet<Guid> ConflictingBookIds { get; } = [];
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
            if (ConflictingBookIds.Contains(book.Id))
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

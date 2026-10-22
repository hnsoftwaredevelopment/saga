using EbookManager.Application.Books;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using FluentAssertions;

namespace EbookManager.Tests.Metadata;

public sealed class MetadataQualityLanguageRepairServiceTests
{
    [Fact]
    public async Task Repair_preserves_a_typed_duplicate_reason_without_writing()
    {
        var book = CreateBook(language: null);
        var repository = new InMemoryBookRepository(book) { ThrowConflict = true };
        var result = await CreateService(repository).RepairAsync([book.Id], "nl", default);
        var item = result.Items.Should().ContainSingle().Subject;
        item.Status.Should().Be(MetadataQualityLanguageRepairStatus.Failed);
        item.FailureReason.Should().Be(MetadataQualityRepairFailureReason.BookAlreadyExists);
        item.Book!.Metadata.Should().Be(book.Metadata);
    }

    [Fact]
    public async Task RepairAsync_normalizes_and_changes_only_the_unknown_language()
    {
        var original = CreateBook(language: null);
        var repository = new InMemoryBookRepository(original);
        var service = CreateService(repository);

        var result = await service.RepairAsync([original.Id], " nl-NL ", default);

        var repaired = result.Items.Should().ContainSingle().Which;
        repaired.Status.Should().Be(MetadataQualityLanguageRepairStatus.Succeeded);
        repaired.Book!.Metadata.Language.Should().Be("nl");
        repaired.Book.Metadata.Title.Should().Be(original.Metadata.Title);
        repaired.Book.Metadata.Authors.Should().Equal(original.Metadata.Authors);
        repaired.Book.Metadata.Tags.Should().Equal(original.Metadata.Tags!);
        repaired.Book.UpdatedUtc.Should().BeAfter(original.UpdatedUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("fictional-language")]
    public async Task RepairAsync_rejects_an_invalid_language_without_writing(string language)
    {
        var original = CreateBook(language: null);
        var repository = new InMemoryBookRepository(original);
        var service = CreateService(repository);

        var action = () => service.RepairAsync([original.Id], language, default);

        await action.Should().ThrowAsync<ArgumentException>();
        repository.UpdateCalls.Should().Be(0);
    }

    [Fact]
    public async Task RepairAsync_does_not_overwrite_an_existing_valid_language()
    {
        var original = CreateBook("en");
        var repository = new InMemoryBookRepository(original);
        var service = CreateService(repository);

        var result = await service.RepairAsync([original.Id], "nl", default);

        result.Items.Should().ContainSingle().Which.Status
            .Should().Be(MetadataQualityLanguageRepairStatus.NotApplicable);
        repository.UpdateCalls.Should().Be(0);
    }

    [Fact]
    public async Task RepairAsync_reports_an_unexpected_book_failure_and_continues_with_the_next_book()
    {
        var first = CreateBook(language: null);
        var failing = CreateBook(language: null);
        var last = CreateBook(language: null);
        var repository = new InMemoryBookRepository(first, failing, last);
        repository.FailingReadBookIds.Add(failing.Id);
        var service = CreateService(repository);

        var result = await service.RepairAsync([first.Id, failing.Id, last.Id], "nl", default);

        result.Items.Should().HaveCount(3);
        result.Items.Single(item => item.BookId == first.Id).Status
            .Should().Be(MetadataQualityLanguageRepairStatus.Succeeded);
        var failed = result.Items.Single(item => item.BookId == failing.Id);
        failed.Status.Should().Be(MetadataQualityLanguageRepairStatus.Failed);
        failed.Message.Should().Be("Read failed.");
        result.Items.Single(item => item.BookId == last.Id).Status
            .Should().Be(MetadataQualityLanguageRepairStatus.Succeeded);
        repository.UpdateCalls.Should().Be(2);
    }

    private static MetadataQualityLanguageRepairService CreateService(InMemoryBookRepository repository) =>
        new(
            repository,
            new BookService(
                repository,
                new NoopLibraryFileStore(),
                new ThrowingMetadataAdapterResolver()));

    private static Book CreateBook(string? language)
    {
        var created = DateTimeOffset.UtcNow.AddDays(-2);
        return new Book(
            Guid.NewGuid(),
            new BookMetadata(
                "Boek",
                ["Auteur"],
                Description: "Beschrijving",
                Language: language,
                Tags: ["Tag"]),
            ReadingStatus.Unread,
            null,
            created,
            created);
    }

    private sealed class InMemoryBookRepository(params Book[] seed) : IBookRepository
    {
        private readonly Dictionary<Guid, Book> books = seed.ToDictionary(book => book.Id);

        public bool ThrowConflict { get; init; }
        public int UpdateCalls { get; private set; }
        public HashSet<Guid> FailingReadBookIds { get; } = [];

        public Task<IReadOnlyList<Book>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Book>>(books.Values.ToArray());

        public Task<Book?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            if (FailingReadBookIds.Contains(id))
            {
                throw new IOException("Read failed.");
            }

            return Task.FromResult(books.GetValueOrDefault(id));
        }

        public Task UpdateAsync(Book updatedBook, CancellationToken cancellationToken)
        {
            if (ThrowConflict) { throw new BookConflictException(); }
            UpdateCalls++;
            books[updatedBook.Id] = updatedBook;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<BookFile>> ListFilesAsync(Guid bookId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BookFile>>([]);

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

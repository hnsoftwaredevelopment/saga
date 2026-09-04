using EbookManager.Application.Books;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using FluentAssertions;

namespace EbookManager.Tests.Metadata;

public sealed class MetadataQualityCoverRepairServiceTests
{
    [Fact]
    public async Task General_update_replaces_an_existing_cover_and_preserves_edited_metadata()
    {
        var original = CreateBook() with
        {
            Metadata = CopyMetadata(CreateBook().Metadata, [9, 9]),
            CoverRelativePath = "books/existing/cover.jpg"
        };
        var repository = new InMemoryBookRepository(original);
        var coverStore = new RecordingCoverStore();
        var service = CreateUpdateService(repository, coverStore);
        var edited = original with { Metadata = CopyMetadata(original.Metadata, [1, 2, 3], "Gewijzigd") };

        var result = await service.UpdateAsync(edited, [1, 2, 3], CancellationToken.None);

        result.SaveResult.Status.Should().Be(BookSaveStatus.Succeeded);
        result.Book!.Metadata.Title.Should().Be("Gewijzigd");
        result.Book.Metadata.CoverBytes.Should().Equal(1, 2, 3);
        result.Book.CoverRelativePath.Should().Be($"books/{original.Id:N}/cover.jpg");
    }

    [Fact]
    public async Task General_update_restores_the_previous_cover_when_database_save_conflicts()
    {
        var original = CreateBook() with
        {
            Metadata = CopyMetadata(CreateBook().Metadata, [9, 9]),
            CoverRelativePath = "books/existing/cover.jpg"
        };
        var repository = new InMemoryBookRepository(original) { ThrowConflict = true };
        var coverStore = new RecordingCoverStore();
        var service = CreateUpdateService(repository, coverStore);

        var result = await service.UpdateAsync(original, [1, 2, 3], CancellationToken.None);

        result.SaveResult.Status.Should().Be(BookSaveStatus.Conflict);
        coverStore.SavedBytes.Should().HaveCount(2);
        coverStore.SavedBytes[0].Should().Equal(1, 2, 3);
        coverStore.SavedBytes[1].Should().Equal(9, 9);
    }

    [Fact]
    public async Task Repair_saves_cover_and_preserves_other_book_data()
    {
        var original = CreateBook();
        var repository = new InMemoryBookRepository(original);
        var coverStore = new RecordingCoverStore();
        var service = CreateService(repository, coverStore);

        var result = await service.RepairAsync(original.Id, [0xFF, 0xD8, 1, 2], CancellationToken.None);

        result.Status.Should().Be(MetadataQualityCoverRepairStatus.Succeeded);
        result.Book!.CoverRelativePath.Should().Be($"books/{original.Id:N}/cover.jpg");
        result.Book.Metadata.CoverBytes.Should().Equal(0xFF, 0xD8, 1, 2);
        result.Book.Metadata.Title.Should().Be(original.Metadata.Title);
        result.Book.Metadata.Authors.Should().Equal(original.Metadata.Authors);
        result.Book.Metadata.Tags.Should().Equal(original.Metadata.Tags!);
        result.Book.ReadingStatus.Should().Be(original.ReadingStatus);
        result.Book.UpdatedUtc.Should().BeAfter(original.UpdatedUtc);
        coverStore.DeleteCalls.Should().Be(0);
    }

    [Fact]
    public async Task Repair_does_not_replace_a_cover_added_after_dashboard_evaluation()
    {
        var existing = CreateBook() with
        {
            Metadata = CopyMetadata(CreateBook().Metadata, [9]),
            CoverRelativePath = "books/existing/cover.jpg"
        };
        var repository = new InMemoryBookRepository(existing);
        var coverStore = new RecordingCoverStore();
        var service = CreateService(repository, coverStore);

        var result = await service.RepairAsync(existing.Id, [1, 2, 3], CancellationToken.None);

        result.Status.Should().Be(MetadataQualityCoverRepairStatus.NotApplicable);
        repository.UpdateCalls.Should().Be(0);
        coverStore.SaveCalls.Should().Be(0);
    }

    [Fact]
    public async Task Repair_deletes_new_file_when_database_update_fails()
    {
        var original = CreateBook();
        var repository = new InMemoryBookRepository(original) { ThrowConflict = true };
        var coverStore = new RecordingCoverStore();
        var service = CreateService(repository, coverStore);

        var result = await service.RepairAsync(original.Id, [1, 2, 3], CancellationToken.None);

        result.Status.Should().Be(MetadataQualityCoverRepairStatus.Failed);
        coverStore.DeleteCalls.Should().Be(1);
        (await repository.GetAsync(original.Id, CancellationToken.None))!.Metadata.CoverBytes.Should().BeNull();
    }

    [Fact]
    public async Task Repair_keeps_file_when_database_was_updated_before_writeback_failed()
    {
        var original = CreateBook();
        var repository = new InMemoryBookRepository(original) { ThrowWhenListingFiles = true };
        var coverStore = new RecordingCoverStore();
        var service = CreateService(repository, coverStore);

        var result = await service.RepairAsync(original.Id, [1, 2, 3], CancellationToken.None);

        result.Status.Should().Be(MetadataQualityCoverRepairStatus.SavedWithWriteBackErrors);
        result.Book!.Metadata.CoverBytes.Should().Equal(1, 2, 3);
        coverStore.DeleteCalls.Should().Be(0);
    }

    [Fact]
    public async Task Repair_cleans_up_the_file_when_saving_is_cancelled_before_database_update()
    {
        var original = CreateBook();
        var repository = new InMemoryBookRepository(original) { CancelWhenUpdating = true };
        var coverStore = new RecordingCoverStore();
        var service = CreateService(repository, coverStore);

        var action = () => service.RepairAsync(original.Id, [1, 2, 3], CancellationToken.None);

        await action.Should().ThrowAsync<OperationCanceledException>();
        coverStore.DeleteCalls.Should().Be(1);
    }

    private static MetadataQualityCoverRepairService CreateService(
        InMemoryBookRepository repository,
        IBookCoverStore coverStore) =>
        new(
            repository,
            new BookService(repository, new NoopLibraryFileStore(), new ThrowingMetadataAdapterResolver()),
            coverStore);

    private static BookCoverUpdateService CreateUpdateService(
        InMemoryBookRepository repository,
        IBookCoverStore coverStore) =>
        new(
            repository,
            new BookService(repository, new NoopLibraryFileStore(), new ThrowingMetadataAdapterResolver()),
            coverStore);

    private static Book CreateBook()
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata(
                "Titel",
                ["Auteur"],
                Description: "Beschrijving",
                Language: "nl",
                Publisher: "Uitgever",
                PublicationDate: new DateOnly(2025, 1, 2),
                Tags: ["Tag"],
                Series: "Serie",
                SeriesNumber: 3,
                Isbn: "9780000000001"),
            ReadingStatus.Reading,
            null,
            now.AddDays(-2),
            now.AddDays(-1));
    }

    private static BookMetadata CopyMetadata(BookMetadata metadata, byte[] coverBytes, string? title = null) =>
        new(
            title ?? metadata.Title,
            metadata.Authors,
            metadata.Description,
            metadata.Language,
            metadata.Publisher,
            metadata.PublicationDate,
            metadata.Tags,
            metadata.Series,
            metadata.SeriesNumber,
            metadata.Isbn,
            coverBytes);

    private sealed class RecordingCoverStore : IBookCoverStore
    {
        public int SaveCalls { get; private set; }
        public int DeleteCalls { get; private set; }
        public List<byte[]> SavedBytes { get; } = [];

        public Task<string> SaveAsync(Guid bookId, byte[] coverBytes, CancellationToken cancellationToken)
        {
            SaveCalls++;
            SavedBytes.Add([.. coverBytes]);
            return Task.FromResult($"books/{bookId:N}/cover.jpg");
        }

        public Task DeleteAsync(Guid bookId, CancellationToken cancellationToken)
        {
            DeleteCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryBookRepository(Book seed) : IBookRepository
    {
        private Book book = seed;
        public int UpdateCalls { get; private set; }
        public bool ThrowConflict { get; init; }
        public bool CancelWhenUpdating { get; init; }
        public bool ThrowWhenListingFiles { get; init; }

        public Task<Book?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Book?>(id == book.Id ? book : null);

        public Task UpdateAsync(Book updatedBook, CancellationToken cancellationToken)
        {
            UpdateCalls++;
            if (CancelWhenUpdating)
            {
                throw new OperationCanceledException();
            }

            if (ThrowConflict)
            {
                throw new BookConflictException();
            }

            book = updatedBook;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<BookFile>> ListFilesAsync(Guid bookId, CancellationToken cancellationToken) =>
            ThrowWhenListingFiles
                ? throw new IOException("Write-back unavailable.")
                : Task.FromResult<IReadOnlyList<BookFile>>([]);

        public Task<IReadOnlyList<Book>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Book>>([book]);
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

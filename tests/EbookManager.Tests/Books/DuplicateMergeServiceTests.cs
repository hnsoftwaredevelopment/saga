using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using FluentAssertions;

namespace EbookManager.Tests.Books;

public sealed class DuplicateMergeServiceTests
{
    [Fact]
    public async Task Merge_formats_only_attaches_source_files_to_target_without_metadata_overwrite()
    {
        var sourceBookId = Guid.NewGuid();
        var targetBookId = Guid.NewGuid();
        var repository = new RecordingBookRepository();
        var service = new DuplicateMergeService(repository);

        var result = await service.MergeFormatsOnlyAsync(
            sourceBookId,
            targetBookId,
            default);

        result.SourceBookId.Should().Be(sourceBookId);
        result.TargetBookId.Should().Be(targetBookId);
        result.MetadataPolicy.Should().Be(DuplicateMergeMetadataPolicy.PreserveTarget);
        repository.AttachedSourceBookId.Should().Be(sourceBookId);
        repository.AttachedTargetBookId.Should().Be(targetBookId);
    }

    [Fact]
    public async Task Merge_updates_target_metadata_from_selected_field_actions()
    {
        var source = CreateBook(
            "Bron titel",
            ["Karin Slaughter", "Tweede Auteur"],
            description: "Bron omschrijving.",
            tags: ["Thriller", "Nieuwe tag"],
            publisher: "Bron uitgever",
            coverBytes: [9, 8, 7]);
        var target = CreateBook(
            "Doel titel",
            ["Karin Slaughter"],
            description: "Doel omschrijving.",
            tags: ["Thriller", "Bestaand"],
            publisher: "Doel uitgever",
            coverBytes: [1, 2, 3]);
        var repository = new RecordingBookRepository(source, target);
        var service = new DuplicateMergeService(repository);

        await service.MergeAsync(
            source.Id,
            target.Id,
            [
                new DuplicateMergeFieldSelection(DuplicateMergeMetadataField.Title, DuplicateMergeAction.NoAction),
                new DuplicateMergeFieldSelection(DuplicateMergeMetadataField.Authors, DuplicateMergeAction.Merge),
                new DuplicateMergeFieldSelection(DuplicateMergeMetadataField.Description, DuplicateMergeAction.Merge),
                new DuplicateMergeFieldSelection(DuplicateMergeMetadataField.Tags, DuplicateMergeAction.Merge),
                new DuplicateMergeFieldSelection(DuplicateMergeMetadataField.Publisher, DuplicateMergeAction.Copy),
                new DuplicateMergeFieldSelection(DuplicateMergeMetadataField.Cover, DuplicateMergeAction.Copy)
            ],
            default);

        repository.AttachedSourceBookId.Should().Be(source.Id);
        repository.AttachedTargetBookId.Should().Be(target.Id);
        repository.UpdatedBook.Should().NotBeNull();
        repository.UpdatedBook!.Metadata.Title.Should().Be("Doel titel");
        repository.UpdatedBook.Metadata.Authors.Should().Equal("Karin Slaughter", "Tweede Auteur");
        repository.UpdatedBook.Metadata.Description.Should().Be("Doel omschrijving.\n\nBron omschrijving.");
        repository.UpdatedBook.Metadata.Tags.Should().Equal("Thriller", "Bestaand", "Nieuwe tag");
        repository.UpdatedBook.Metadata.Publisher.Should().Be("Bron uitgever");
        repository.UpdatedBook.Metadata.CoverBytes.Should().Equal(9, 8, 7);
    }

    [Fact]
    public async Task Merge_ignores_format_selection_because_files_are_linked_by_the_merge_operation()
    {
        var source = CreateBook("Bron titel", ["Auteur"]);
        var target = CreateBook("Doel titel", ["Auteur"]);
        var repository = new RecordingBookRepository(source, target);
        var service = new DuplicateMergeService(repository);

        await service.MergeAsync(
            source.Id,
            target.Id,
            [new DuplicateMergeFieldSelection(DuplicateMergeMetadataField.Formats, DuplicateMergeAction.Merge)],
            default);

        repository.AttachedSourceBookId.Should().Be(source.Id);
        repository.UpdatedBook!.Formats.Should().Equal(target.Formats);
    }

    [Fact]
    public async Task Merge_does_not_clear_target_cover_when_source_has_no_cover()
    {
        var source = CreateBook("Bron titel", ["Auteur"]);
        var target = CreateBook("Doel titel", ["Auteur"], coverBytes: [1, 2, 3]);
        var repository = new RecordingBookRepository(source, target);
        var service = new DuplicateMergeService(repository);

        await service.MergeAsync(
            source.Id,
            target.Id,
            [new DuplicateMergeFieldSelection(DuplicateMergeMetadataField.Cover, DuplicateMergeAction.Copy)],
            default);

        repository.UpdatedBook!.CoverRelativePath.Should().Be(target.CoverRelativePath);
        repository.UpdatedBook.Metadata.CoverBytes.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task Merge_writes_final_metadata_once_to_each_linked_book_directory()
    {
        var source = CreateBook("Bron titel", ["Bron auteur"]);
        var target = CreateBook("Doel titel", ["Doel auteur"]);
        var files = new[]
        {
            CreateFile(source.Id, "books/aa/source/book.pdf", EbookFormat.Pdf),
            CreateFile(target.Id, "books/bb/target/book.epub", EbookFormat.Epub),
            CreateFile(target.Id, "books/bb/target/book.mobi", EbookFormat.Mobi)
        };
        var repository = new RecordingBookRepository([source, target], files);
        var fileStore = new TestLibraryFileStore("C:/library");
        var sidecarStore = new RecordingMetadataSidecarStore();
        var service = new DuplicateMergeService(repository, fileStore, sidecarStore);

        var result = await service.MergeAsync(
            source.Id,
            target.Id,
            [new DuplicateMergeFieldSelection(DuplicateMergeMetadataField.Authors, DuplicateMergeAction.Copy)],
            default);

        result.SidecarStatus.Should().Be(DuplicateMergeSidecarStatus.Succeeded);
        sidecarStore.Writes.Should().HaveCount(2);
        sidecarStore.Writes.Select(write => Path.GetDirectoryName(write.BookFilePath))
            .Should().BeEquivalentTo(
                Path.GetFullPath("C:/library/books/aa/source"),
                Path.GetFullPath("C:/library/books/bb/target"));
        sidecarStore.Writes.Should().OnlyContain(write =>
            write.Metadata.Authors.SequenceEqual(new[] { "Bron auteur" }));
    }

    [Fact]
    public async Task Merge_reports_sidecar_failure_after_database_merge_succeeds()
    {
        var source = CreateBook("Bron titel", ["Auteur"]);
        var target = CreateBook("Doel titel", ["Auteur"]);
        var repository = new RecordingBookRepository(
            [source, target],
            [CreateFile(target.Id, "books/target/book.epub", EbookFormat.Epub)]);
        var service = new DuplicateMergeService(
            repository,
            new TestLibraryFileStore("C:/library"),
            new ThrowingMetadataSidecarStore());

        var result = await service.MergeAsync(source.Id, target.Id, [], default);

        result.SidecarStatus.Should().Be(DuplicateMergeSidecarStatus.Failed);
        result.SidecarMessage.Should().Be("sidecar unavailable");
        repository.UpdatedBook.Should().NotBeNull();
    }

    private sealed class RecordingBookRepository : IBookRepository
    {
        private readonly Dictionary<Guid, Book> books = [];

        private readonly List<BookFile> files;

        public RecordingBookRepository(params Book[] books)
            : this(books, [])
        {
        }

        public RecordingBookRepository(IEnumerable<Book> books, IEnumerable<BookFile> files)
        {
            foreach (var book in books)
            {
                this.books.Add(book.Id, book);
            }

            this.files = files.ToList();
        }

        public Guid? AttachedSourceBookId { get; private set; }
        public Guid? AttachedTargetBookId { get; private set; }
        public Book? UpdatedBook { get; private set; }

        public Task<IReadOnlyList<Book>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Book>>([]);

        public Task<Book?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(books.GetValueOrDefault(id));

        public Task<bool> HasHashAsync(string sha256, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> HasNormalizedTitleAndAuthorAsync(
            string title,
            IReadOnlyList<string> authors,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<Book?> FindByNormalizedTitleAndAuthorAsync(
            string title,
            IReadOnlyList<string> authors,
            CancellationToken cancellationToken) =>
            Task.FromResult<Book?>(null);

        public Task<IReadOnlyList<Book>> FindByNormalizedTitleAsync(
            string title,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Book>>([]);

        public Task AddAsync(Book book, BookFile file, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AddFileAsync(BookFile file, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AttachFilesToBookAsync(
            Guid sourceBookId,
            Guid targetBookId,
            CancellationToken cancellationToken)
        {
            AttachedSourceBookId = sourceBookId;
            AttachedTargetBookId = targetBookId;
            for (var index = 0; index < files.Count; index++)
            {
                if (files[index].BookId == sourceBookId)
                {
                    files[index] = files[index] with { BookId = targetBookId };
                }
            }

            return Task.CompletedTask;
        }

        public Task UpdateAsync(Book book, CancellationToken cancellationToken)
        {
            UpdatedBook = book;
            books[book.Id] = book;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<BookFileDeleteRepositoryResult> DeleteFileAsync(
            Guid bookId,
            Guid fileId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new BookFileDeleteRepositoryResult(BookFileDeleteRepositoryStatus.NotFound));

        public Task<IReadOnlyList<BookFile>> ListFilesAsync(Guid bookId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BookFile>>(files.Where(file => file.BookId == bookId).ToArray());

        public Task UpdateFileWriteBackAsync(
            Guid fileId,
            MetadataWriteResult result,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class TestLibraryFileStore(string rootPath) : ILibraryFileStore
    {
        public string GetAbsolutePath(string relativePath) =>
            Path.GetFullPath(Path.Combine(rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        public Task<(string RelativeBookPath, string? RelativeCoverPath)> CopyIntoLibraryAsync(
            Guid bookId,
            string sourcePath,
            byte[]? coverBytes,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteBookDirectoryAsync(Guid bookId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingMetadataSidecarStore : IMetadataSidecarStore
    {
        public List<(string BookFilePath, BookMetadata Metadata)> Writes { get; } = [];

        public Task<BookMetadata?> TryReadAsync(string bookFilePath, CancellationToken cancellationToken) =>
            Task.FromResult<BookMetadata?>(null);

        public Task WriteAsync(
            string bookFilePath,
            BookMetadata metadata,
            CancellationToken cancellationToken)
        {
            Writes.Add((bookFilePath, metadata));
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingMetadataSidecarStore : IMetadataSidecarStore
    {
        public Task<BookMetadata?> TryReadAsync(string bookFilePath, CancellationToken cancellationToken) =>
            Task.FromResult<BookMetadata?>(null);

        public Task WriteAsync(
            string bookFilePath,
            BookMetadata metadata,
            CancellationToken cancellationToken) =>
            throw new IOException("sidecar unavailable");
    }

    private static BookFile CreateFile(Guid bookId, string relativePath, EbookFormat format) =>
        new(
            Guid.NewGuid(),
            bookId,
            format,
            relativePath,
            Guid.NewGuid().ToString("N"),
            123,
            MetadataWriteBackStatus.NotAttempted,
            null);

    private static Book CreateBook(
        string title,
        IReadOnlyList<string> authors,
        string? description = null,
        IReadOnlyList<string>? tags = null,
        string? publisher = null,
        byte[]? coverBytes = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata(
                title,
                authors,
                Description: description,
                Publisher: publisher,
                Tags: tags,
                CoverBytes: coverBytes),
            ReadingStatus.Unread,
            coverBytes is null ? null : $"books/{Guid.NewGuid():N}/cover.jpg",
            now,
            now)
        {
            Formats = [EbookFormat.Epub]
        };
    }
}

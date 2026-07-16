using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;
using System.Globalization;

namespace EbookManager.Tests.App.ViewModels;

public sealed class BookDetailsViewModelTests
{
    [Fact]
    public void Loading_a_book_does_not_set_dirty_state()
    {
        var viewModel = CreateViewModel(out _);
        var book = CreateBook("Original", ["First Author"]);

        viewModel.Load(book);

        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public void Loading_a_book_with_metadata_whitespace_does_not_set_dirty_state()
    {
        var viewModel = CreateViewModel(out _);
        var now = DateTimeOffset.UtcNow;
        var book = new Book(
            Guid.NewGuid(),
            new BookMetadata(
                " Original ",
                [" First Author "],
                Description: " Description ",
                Language: " en ",
                Publisher: " Publisher ",
                Tags: [" Tag "],
                Series: " Series ",
                Isbn: " 9780000000000 "),
            ReadingStatus.Unread,
            null,
            now,
            now);

        viewModel.Load(book);

        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public void Editing_metadata_sets_dirty_state_and_undo_restores_original_values()
    {
        var viewModel = CreateViewModel(out _);
        var book = CreateBook("Original", ["First Author"]);

        viewModel.Load(book);
        viewModel.Title = "Changed";

        viewModel.HasUnsavedChanges.Should().BeTrue();
        viewModel.UndoCommand.Execute(null);

        viewModel.Title.Should().Be("Original");
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public void Loading_a_book_shows_available_formats_without_setting_dirty_state()
    {
        var viewModel = CreateViewModel(out _);
        var book = CreateBook("Original", ["First Author"], [EbookFormat.Pdf, EbookFormat.Epub]);

        viewModel.Load(book);

        viewModel.FormatsText.Should().Be("EPUB, PDF");
        viewModel.FormatDetails.Select(format => format.DisplayText).Should().Equal("EPUB", "PDF");
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public async Task Loading_format_details_shows_file_sizes_per_available_format()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var viewModel = CreateViewModel(out var repository);
            var book = CreateBook("Original", ["First Author"], [EbookFormat.Epub, EbookFormat.Pdf]);
            repository.SeedFiles(
                book.Id,
                [
                    CreateBookFile(book.Id, EbookFormat.Pdf, "books/book/original.pdf", 4_404_019),
                    CreateBookFile(book.Id, EbookFormat.Epub, "books/book/original.epub", 1_887_436)
                ]);

            viewModel.Load(book);
            await viewModel.LoadFormatDetailsAsync(book.Id);

            viewModel.FormatDetails.Select(format => format.DisplayText)
                .Should()
                .Equal("EPUB - 1.8 MB", "PDF - 4.2 MB");
            viewModel.FormatDetails.Should().AllSatisfy(format => format.FileId.Should().NotBeNull());
            viewModel.HasUnsavedChanges.Should().BeFalse();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Load_exposes_standard_metadata_fields()
    {
        var viewModel = CreateViewModel(out _);
        var created = new DateTimeOffset(2026, 7, 15, 10, 30, 0, TimeSpan.Zero);
        var updated = new DateTimeOffset(2026, 7, 16, 11, 45, 0, TimeSpan.Zero);
        var book = new Book(
            Guid.NewGuid(),
            new BookMetadata(
                "Title",
                ["Author"],
                "Description",
                "nl",
                "Publisher",
                new DateOnly(2020, 1, 2),
                ["Tag"],
                "Series",
                1,
                "9780000000000"),
            ReadingStatus.Read,
            null,
            created,
            updated)
        {
            Formats = [EbookFormat.Epub, EbookFormat.Pdf]
        };

        viewModel.Load(book);

        viewModel.Title.Should().Be("Title");
        viewModel.AuthorsText.Should().Be("Author");
        viewModel.Description.Should().Be("Description");
        viewModel.Language.Should().Be("nl");
        viewModel.Publisher.Should().Be("Publisher");
        viewModel.PublicationDate.Should().Be(new DateOnly(2020, 1, 2));
        viewModel.TagsText.Should().Be("Tag");
        viewModel.Series.Should().Be("Series");
        viewModel.SeriesNumber.Should().Be(1);
        viewModel.Isbn.Should().Be("9780000000000");
        viewModel.FormatsText.Should().Be("EPUB, PDF");
        viewModel.ReadingStatus.Should().Be(ReadingStatus.Read);
        viewModel.CreatedUtcText.Should().Be(created.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
        viewModel.UpdatedUtcText.Should().Be(updated.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Load_exposes_friendly_language_display_without_changing_stored_value()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");
        try
        {
            var viewModel = CreateViewModel(out _);
            var book = CreateBook("Original", ["First Author"], language: "eng");

            viewModel.Load(book);

            viewModel.Language.Should().Be("eng");
            viewModel.LanguageDisplayName.Should().Be("Engels");
            viewModel.HasUnsavedChanges.Should().BeFalse();

            viewModel.Language = "nl-NL";

            viewModel.Language.Should().Be("nl-NL");
            viewModel.LanguageDisplayName.Should().Be("Nederlands");
            viewModel.HasUnsavedChanges.Should().BeTrue();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Refresh_localized_display_names_updates_language_display_without_dirtying_book()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("nl-NL");
            var viewModel = CreateViewModel(out _);
            var book = CreateBook("Original", ["First Author"], language: "nl") with
            {
                CreatedUtc = new DateTimeOffset(2026, 7, 15, 10, 30, 0, TimeSpan.Zero),
                UpdatedUtc = new DateTimeOffset(2026, 7, 16, 11, 45, 0, TimeSpan.Zero)
            };
            viewModel.Load(book);

            viewModel.LanguageDisplayName.Should().Be("Nederlands");
            var originalCreatedText = viewModel.CreatedUtcText;
            viewModel.HasUnsavedChanges.Should().BeFalse();

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            viewModel.RefreshLocalizedDisplayNames();

            viewModel.Language.Should().Be("nl");
            viewModel.LanguageDisplayName.Should().Be("Dutch");
            viewModel.CreatedUtcText.Should().NotBe(originalCreatedText);
            viewModel.HasUnsavedChanges.Should().BeFalse();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public async Task Save_updates_metadata_and_clears_dirty_state()
    {
        var viewModel = CreateViewModel(out var repository);
        var book = CreateBook("Original", ["First Author"]);
        viewModel.Load(book);
        viewModel.Title = "Changed";
        viewModel.AuthorsText = "Second Author; Third Author";
        viewModel.ReadingStatus = ReadingStatus.Read;

        await viewModel.SaveCommand.ExecuteAsync(null);

        repository.UpdatedBook.Should().NotBeNull();
        repository.UpdatedBook!.Metadata.Title.Should().Be("Changed");
        repository.UpdatedBook.Metadata.Authors.Should().Equal("Second Author", "Third Author");
        repository.UpdatedBook.ReadingStatus.Should().Be(ReadingStatus.Read);
        viewModel.LastSaveResult!.Status.Should().Be(BookSaveStatus.Succeeded);
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public async Task Save_conflict_keeps_dirty_state_and_exposes_save_error()
    {
        var viewModel = CreateViewModel(out var repository);
        repository.ThrowConflictOnUpdate = true;
        var book = CreateBook("Original", ["First Author"]);
        viewModel.Load(book);
        viewModel.AuthorsText = "Second Author";

        await viewModel.SaveCommand.ExecuteAsync(null);

        viewModel.LastSaveResult!.Status.Should().Be(BookSaveStatus.Conflict);
        viewModel.HasSaveError.Should().BeTrue();
        viewModel.SaveErrorMessage.Should().Be("A book with the same title and author already exists.");
        viewModel.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_removes_loaded_book_and_clears_details()
    {
        var viewModel = CreateViewModel(out var repository);
        var book = CreateBook("Original", ["First Author"]);
        viewModel.Load(book);

        await viewModel.DeleteCommand.ExecuteAsync(null);

        repository.DeletedBookId.Should().Be(book.Id);
        viewModel.BookId.Should().BeNull();
        viewModel.LastDeleteResult.Should().BeNull();
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    private static BookDetailsViewModel CreateViewModel(out RecordingBookRepository repository)
    {
        repository = new RecordingBookRepository();
        var service = new BookService(
            repository,
            new NoopLibraryFileStore(),
            new NoopMetadataAdapterResolver());
        return new BookDetailsViewModel(service);
    }

    private static Book CreateBook(
        string title,
        IReadOnlyList<string> authors,
        IReadOnlyList<EbookFormat>? formats = null,
        string? language = "en")
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata(
                title,
                authors,
                Description: "Description",
                Language: language,
                Publisher: "Publisher",
                Tags: ["Tag"],
                Isbn: "9780000000000"),
            ReadingStatus.Unread,
            null,
            now,
            now)
        {
            Formats = formats ?? []
        };
    }

    private static BookFile CreateBookFile(
        Guid bookId,
        EbookFormat format,
        string relativePath,
        long sizeBytes) =>
        new(
            Guid.NewGuid(),
            bookId,
            format,
            relativePath,
            new string('a', 64),
            sizeBytes,
            MetadataWriteBackStatus.Unsupported,
            null);

    private sealed class RecordingBookRepository : IBookRepository
    {
        private readonly Dictionary<Guid, IReadOnlyList<BookFile>> filesByBookId = [];
        public Book? UpdatedBook { get; private set; }
        public Guid? DeletedBookId { get; private set; }
        public bool ThrowConflictOnUpdate { get; set; }

        public void SeedFiles(Guid bookId, IReadOnlyList<BookFile> files)
        {
            filesByBookId[bookId] = files;
        }

        public Task<IReadOnlyList<Book>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Book>>([]);
        public Task<Book?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Book?>(null);
        public Task<bool> HasHashAsync(string sha256, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> HasNormalizedTitleAndAuthorAsync(string title, IReadOnlyList<string> authors, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<Book?> FindByNormalizedTitleAndAuthorAsync(string title, IReadOnlyList<string> authors, CancellationToken cancellationToken) => Task.FromResult<Book?>(null);
        public Task<IReadOnlyList<Book>> FindByNormalizedTitleAsync(string title, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Book>>([]);
        public Task AddAsync(Book book, BookFile file, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddFileAsync(BookFile file, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AttachFilesToBookAsync(Guid sourceBookId, Guid targetBookId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateAsync(Book book, CancellationToken cancellationToken)
        {
            if (ThrowConflictOnUpdate)
            {
                throw new BookConflictException();
            }

            UpdatedBook = book;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            DeletedBookId = id;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<BookFile>> ListFilesAsync(Guid bookId, CancellationToken cancellationToken) =>
            Task.FromResult(filesByBookId.TryGetValue(bookId, out var files)
                ? files
                : (IReadOnlyList<BookFile>)[]);

        public Task UpdateFileWriteBackAsync(Guid fileId, MetadataWriteResult result, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class NoopLibraryFileStore : ILibraryFileStore
    {
        public Task<(string RelativeBookPath, string? RelativeCoverPath)> CopyIntoLibraryAsync(
            Guid bookId,
            string sourcePath,
            byte[]? coverBytes,
            CancellationToken cancellationToken) =>
            Task.FromResult(($"books/{bookId:N}/book.epub", (string?)null));

        public Task DeleteBookDirectoryAsync(Guid bookId, CancellationToken cancellationToken) => Task.CompletedTask;
        public string GetAbsolutePath(string relativePath) => relativePath;
    }

    private sealed class NoopMetadataAdapterResolver : IMetadataAdapterResolver
    {
        public IMetadataAdapter Resolve(EbookFormat format) => new NoopMetadataAdapter();
    }

    private sealed class NoopMetadataAdapter : IMetadataAdapter
    {
        public bool CanHandle(EbookFormat format) => true;

        public Task<MetadataReadResult> ReadAsync(string path, EbookFormat format, CancellationToken cancellationToken) =>
            Task.FromResult(new MetadataReadResult(new BookMetadata("Title", ["Author"])));

        public Task<MetadataWriteResult> WriteAsync(
            string path,
            EbookFormat format,
            BookMetadata metadata,
            CancellationToken cancellationToken) =>
            Task.FromResult(new MetadataWriteResult(MetadataWriteBackStatus.Unsupported));
    }
}

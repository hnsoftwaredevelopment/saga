using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using FluentAssertions;

namespace EbookManager.Tests.Books;

public sealed class BookFileExportServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"saga-export-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task ExportAsync_copies_file_with_metadata_based_filename()
    {
        var source = CreateSourceFile("books/book/original.epub", "content");
        var destination = Directory.CreateDirectory(Path.Combine(root, "Downloads")).FullName;
        var book = CreateBook("A Good Book", ["Ada Lovelace"]);
        var file = CreateBookFile(book.Id, "books/book/original.epub", EbookFormat.Epub);
        var service = new BookFileExportService(new RootedFileStore(Path.Combine(root, "Library")));

        var result = await service.ExportAsync(book, file, destination);

        result.Status.Should().Be(BookFileExportStatus.Exported);
        result.DestinationPath.Should().Be(Path.Combine(destination, "Ada Lovelace - A Good Book.epub"));
        File.ReadAllText(result.DestinationPath!).Should().Be(File.ReadAllText(source));
    }

    [Fact]
    public async Task ExportAsync_sanitizes_invalid_filename_characters()
    {
        CreateSourceFile("books/book/original.pdf", "content");
        var destination = Directory.CreateDirectory(Path.Combine(root, "Downloads")).FullName;
        var book = CreateBook("Bad:Title?", ["Author/Name"]);
        var file = CreateBookFile(book.Id, "books/book/original.pdf", EbookFormat.Pdf);
        var service = new BookFileExportService(new RootedFileStore(Path.Combine(root, "Library")));

        var result = await service.ExportAsync(book, file, destination);

        result.Status.Should().Be(BookFileExportStatus.Exported);
        Path.GetFileName(result.DestinationPath).Should().Be("Author_Name - Bad_Title_.pdf");
    }

    [Fact]
    public async Task ExportAsync_creates_unique_filename_when_destination_exists()
    {
        CreateSourceFile("books/book/original.cbz", "content");
        var destination = Directory.CreateDirectory(Path.Combine(root, "Downloads")).FullName;
        File.WriteAllText(Path.Combine(destination, "Author - Title.cbz"), "existing");
        var book = CreateBook("Title", ["Author"]);
        var file = CreateBookFile(book.Id, "books/book/original.cbz", EbookFormat.Cbz);
        var service = new BookFileExportService(new RootedFileStore(Path.Combine(root, "Library")));

        var result = await service.ExportAsync(book, file, destination);

        result.Status.Should().Be(BookFileExportStatus.Exported);
        Path.GetFileName(result.DestinationPath).Should().Be("Author - Title (1).cbz");
    }

    [Fact]
    public async Task ExportAsync_returns_source_missing_when_managed_file_is_missing()
    {
        var destination = Directory.CreateDirectory(Path.Combine(root, "Downloads")).FullName;
        var book = CreateBook("Title", ["Author"]);
        var file = CreateBookFile(book.Id, "books/book/missing.epub", EbookFormat.Epub);
        var service = new BookFileExportService(new RootedFileStore(Path.Combine(root, "Library")));

        var result = await service.ExportAsync(book, file, destination);

        result.Status.Should().Be(BookFileExportStatus.SourceMissing);
        result.DestinationPath.Should().BeNull();
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private string CreateSourceFile(string relativePath, string content)
    {
        var path = Path.Combine(root, "Library", relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    private static Book CreateBook(string title, IReadOnlyList<string> authors)
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata(title, authors),
            ReadingStatus.Unread,
            null,
            now,
            now);
    }

    private static BookFile CreateBookFile(Guid bookId, string relativePath, EbookFormat format) =>
        new(
            Guid.NewGuid(),
            bookId,
            format,
            relativePath,
            new string('a', 64),
            10,
            MetadataWriteBackStatus.Unsupported,
            null);

    private sealed class RootedFileStore(string rootPath) : ILibraryFileStore
    {
        public string GetAbsolutePath(string relativePath) => Path.Combine(rootPath, relativePath);

        public Task<(string RelativeBookPath, string? RelativeCoverPath)> CopyIntoLibraryAsync(
            Guid bookId,
            string sourcePath,
            byte[]? coverBytes,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteBookDirectoryAsync(Guid bookId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

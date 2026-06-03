using System.IO.Compression;
using System.Text;
using EbookManager.Application.Books;
using EbookManager.Domain.Books;
using EbookManager.Domain.Importing;
using EbookManager.Infrastructure.Persistence;
using EbookManager.Tests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Tests.Integration;

public sealed class VerticalSliceTests
{
    [Fact]
    public async Task Import_search_edit_and_delete_epub_through_sqlite_library()
    {
        await using var fixture = await ImportServiceFixture.CreateAsync();
        var sourcePath = CreateMinimalEpub(
            fixture,
            "The Hobbit",
            "J.R.R. Tolkien",
            [0x01, 0x02, 0x03]);
        var importService = fixture.CreateService();

        var importResult = await importService.ImportAsync([sourcePath], default);

        importResult.Items.Should().ContainSingle().Which.Outcome.Should().Be(ImportOutcome.Added);
        var importedBookId = importResult.Items.Single().BookId!.Value;
        await using (var reopenedContext = fixture.ContextFactory.Create(fixture.LibraryPath))
        {
            (await reopenedContext.Books.AsNoTracking().SingleAsync()).Title.Should().Be("The Hobbit");
        }

        var importedBook = (await fixture.BookRepository.GetAsync(importedBookId, default))!;
        var searchResult = new BookSearchService().Filter([importedBook], "tolkien");
        searchResult.Should().ContainSingle(book => book.Id == importedBookId);

        var bookService = new BookService(
            fixture.BookRepository,
            fixture.FileStore,
            fixture.MetadataAdapterResolver);
        var editedBook = importedBook with
        {
            Metadata = new BookMetadata(
                "The Hobbit Annotated",
                importedBook.Metadata.Authors,
                importedBook.Metadata.Description,
                importedBook.Metadata.Language,
                importedBook.Metadata.Publisher,
                importedBook.Metadata.PublicationDate,
                importedBook.Metadata.Tags,
                importedBook.Metadata.Series,
                importedBook.Metadata.SeriesNumber,
                importedBook.Metadata.Isbn,
                importedBook.Metadata.CoverBytes),
            ReadingStatus = ReadingStatus.Read,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        var saveResult = await bookService.SaveAsync(editedBook, default);

        saveResult.Status.Should().Be(BookSaveStatus.Succeeded);
        saveResult.FileResults.Should()
            .ContainSingle()
            .Which.Result.Status.Should().Be(MetadataWriteBackStatus.Unsupported);
        var reloadedEditedBook = (await fixture.BookRepository.GetAsync(importedBookId, default))!;
        reloadedEditedBook.Metadata.Title.Should().Be("The Hobbit Annotated");
        reloadedEditedBook.ReadingStatus.Should().Be(ReadingStatus.Read);
        (await fixture.BookRepository.ListFilesAsync(importedBookId, default))
            .Should()
            .ContainSingle()
            .Which.WriteBackStatus.Should().Be(MetadataWriteBackStatus.Unsupported);

        var managedBookDirectory = Path.Combine(fixture.LibraryPath, "books", importedBookId.ToString("N"));
        Directory.Exists(managedBookDirectory).Should().BeTrue();

        var deleteResult = await bookService.DeleteAsync(importedBookId, default);

        deleteResult.Status.Should().Be(BookDeleteStatus.Deleted);
        (await fixture.BookRepository.GetAsync(importedBookId, default)).Should().BeNull();
        Directory.Exists(managedBookDirectory).Should().BeFalse();
    }

    private static string CreateMinimalEpub(
        ImportServiceFixture fixture,
        string title,
        string author,
        byte[] coverBytes)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.epub");
        using var stream = File.Create(path);
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false))
        {
            AddTextEntry(archive, "META-INF/container.xml", """
                <?xml version="1.0" encoding="utf-8"?>
                <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
                  <rootfiles>
                    <rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml" />
                  </rootfiles>
                </container>
                """);
            AddTextEntry(archive, "OEBPS/content.opf", $$"""
                <?xml version="1.0" encoding="utf-8"?>
                <package xmlns="http://www.idpf.org/2007/opf" version="3.0" unique-identifier="bookid">
                  <metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
                    <dc:title>{{title}}</dc:title>
                    <dc:creator>{{author}}</dc:creator>
                    <dc:language>en</dc:language>
                    <dc:publisher>Allen &amp; Unwin</dc:publisher>
                    <dc:description>A tested vertical slice.</dc:description>
                    <dc:identifier id="bookid">9780000000000</dc:identifier>
                    <meta name="cover" content="cover-image" />
                  </metadata>
                  <manifest>
                    <item id="cover-image" href="cover.jpg" media-type="image/jpeg" properties="cover-image" />
                  </manifest>
                </package>
                """);
            AddBinaryEntry(archive, "OEBPS/cover.jpg", coverBytes);
        }

        return path;
    }

    private static void AddTextEntry(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }

    private static void AddBinaryEntry(ZipArchive archive, string entryName, byte[] content)
    {
        var entry = archive.CreateEntry(entryName);
        using var entryStream = entry.Open();
        entryStream.Write(content);
    }
}

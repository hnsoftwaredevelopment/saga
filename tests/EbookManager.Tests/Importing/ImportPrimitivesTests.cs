using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using EbookManager.Application.Importing;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using EbookManager.Infrastructure.Files;
using EbookManager.Infrastructure.Metadata;
using EbookManager.Tests.TestSupport;
using FluentAssertions;

namespace EbookManager.Tests.Importing;

public sealed class ImportPrimitivesTests : IDisposable
{
    private readonly TemporaryDirectory temporaryDirectory = new();

    [Fact]
    public void Scanner_respects_recursive_flag_and_sorts_matches()
    {
        var root = temporaryDirectory.DirectoryPath;
        var nested = Path.Combine(root, "nested");
        Directory.CreateDirectory(nested);

        var first = WriteTextFile(Path.Combine(root, "alpha.epub"));
        var second = WriteTextFile(Path.Combine(nested, "zeta.epub"));
        WriteTextFile(Path.Combine(root, "ignore.txt"));

        var scanner = new DirectoryScanner();

        scanner.Scan(root, recursive: false).Should().Equal(first);
        scanner.Scan(root, recursive: true).Should().Equal(first, second);
    }

    [Fact]
    public void Scanner_observes_cancellation_before_work()
    {
        var scanner = new DirectoryScanner();
        var cancellationToken = new CancellationToken(canceled: true);

        var act = () => scanner.Scan(temporaryDirectory.DirectoryPath, recursive: true, cancellationToken);

        act.Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public async Task Hasher_returns_stable_uppercase_sha256()
    {
        var path = WriteBytesFile("ebook-manager.txt", Encoding.UTF8.GetBytes("ebook-manager"));
        var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("ebook-manager")));
        var hasher = new Sha256FileHasher();

        var actual = await hasher.ComputeSha256Async(path, default);

        actual.Should().Be(expected);
    }

    [Fact]
    public async Task Hasher_observes_cancellation_before_work()
    {
        var path = WriteBytesFile("cancelled.txt", Encoding.UTF8.GetBytes("cancelled"));
        var hasher = new Sha256FileHasher();

        var act = () => hasher.ComputeSha256Async(path, new CancellationToken(canceled: true));

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Managed_store_copies_files_under_book_directory_and_deletes_only_book_directory()
    {
        var libraryRoot = Path.Combine(temporaryDirectory.DirectoryPath, "Library");
        Directory.CreateDirectory(libraryRoot);
        var sourcePath = WriteBytesFile("source.epub", [1, 2, 3, 4]);
        var store = new ManagedLibraryFileStore(libraryRoot);
        var bookId = Guid.NewGuid();

        var (relativeBookPath, relativeCoverPath) = await store.CopyIntoLibraryAsync(
            bookId,
            sourcePath,
            [5, 6, 7],
            default);

        relativeBookPath.Should().Be($"books/{bookId:N}/source.epub");
        relativeCoverPath.Should().Be($"books/{bookId:N}/cover.jpg");
        File.Exists(Path.Combine(libraryRoot, relativeBookPath)).Should().BeTrue();
        File.Exists(Path.Combine(libraryRoot, relativeCoverPath!)).Should().BeTrue();

        await store.DeleteBookDirectoryAsync(bookId, default);

        Directory.Exists(Path.Combine(libraryRoot, "books", bookId.ToString("N"))).Should().BeFalse();
        Directory.Exists(Path.Combine(libraryRoot, "books")).Should().BeTrue();
        Directory.Exists(libraryRoot).Should().BeTrue();
    }

    [Fact]
    public async Task Managed_store_observes_cancellation_before_copying()
    {
        var libraryRoot = Path.Combine(temporaryDirectory.DirectoryPath, "Library");
        Directory.CreateDirectory(libraryRoot);
        var sourcePath = WriteBytesFile("cancel-source.epub", [1, 2, 3]);
        var store = new ManagedLibraryFileStore(libraryRoot);

        var act = () => store.CopyIntoLibraryAsync(
            Guid.NewGuid(),
            sourcePath,
            [9, 8, 7],
            new CancellationToken(canceled: true));

        await act.Should().ThrowAsync<OperationCanceledException>();
        Directory.Exists(Path.Combine(libraryRoot, "books")).Should().BeFalse();
    }

    [Theory]
    [InlineData("The Hobbit - J.R.R. Tolkien.epub", "The Hobbit", "J.R.R. Tolkien")]
    [InlineData("Unknown Title.pdf", "Unknown Title", "Unknown")]
    public async Task Fallback_adapter_extracts_filename_metadata(
        string filename,
        string expectedTitle,
        string expectedAuthor)
    {
        var path = WriteBytesFile(filename, [1, 2, 3]);
        var adapter = new FallbackMetadataAdapter();

        var result = await adapter.ReadAsync(path, EbookFormat.Pdf, default);

        result.Metadata.Title.Should().Be(expectedTitle);
        result.Metadata.Authors.Should().Equal(expectedAuthor);
        result.Warning.Should().BeNull();
    }

    [Fact]
    public async Task Fallback_adapter_reports_unsupported_write()
    {
        var adapter = new FallbackMetadataAdapter();

        var result = await adapter.WriteAsync(
            WriteBytesFile("book.epub", [1, 2, 3]),
            EbookFormat.Epub,
            new BookMetadata("Title", ["Author"]),
            default);

        result.Status.Should().Be(MetadataWriteBackStatus.Unsupported);
    }

    [Fact]
    public async Task Epub_adapter_reads_metadata_and_embedded_cover()
    {
        byte[] coverBytes = [0x01, 0x02, 0x03, 0x04];
        var path = CreateArchive("sample.epub", archive =>
        {
            AddTextEntry(archive, "META-INF/container.xml", """
                <?xml version="1.0" encoding="utf-8"?>
                <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
                  <rootfiles>
                    <rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml" />
                  </rootfiles>
                </container>
                """);

            AddTextEntry(archive, "OEBPS/content.opf", """
                <?xml version="1.0" encoding="utf-8"?>
                <package xmlns="http://www.idpf.org/2007/opf" version="3.0" unique-identifier="bookid">
                  <metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
                    <dc:title>The Hobbit</dc:title>
                    <dc:creator>J.R.R. Tolkien</dc:creator>
                    <dc:language>en</dc:language>
                    <dc:publisher>Allen &amp; Unwin</dc:publisher>
                    <dc:description>One ring to rule them all.</dc:description>
                    <dc:identifier id="bookid">9780000000000</dc:identifier>
                    <meta name="cover" content="cover-image" />
                  </metadata>
                  <manifest>
                    <item id="cover-image" href="cover.jpg" media-type="image/jpeg" properties="cover-image" />
                  </manifest>
                </package>
                """);

            AddBinaryEntry(archive, "OEBPS/cover.jpg", coverBytes);
        });
        var adapter = new EpubMetadataAdapter();

        var result = await adapter.ReadAsync(path, EbookFormat.Epub, default);

        result.Metadata.Title.Should().Be("The Hobbit");
        result.Metadata.Authors.Should().Equal("J.R.R. Tolkien");
        result.Metadata.Language.Should().Be("en");
        result.Metadata.Publisher.Should().Be("Allen & Unwin");
        result.Metadata.Description.Should().Be("One ring to rule them all.");
        result.Metadata.Isbn.Should().Be("9780000000000");
        result.Metadata.CoverBytes.Should().Equal(coverBytes);
        result.Warning.Should().BeNull();
    }

    [Fact]
    public async Task Epub_adapter_returns_filename_fallback_with_warning_for_malformed_archive()
    {
        var path = CreateArchive("Malformed.epub", archive =>
        {
            AddTextEntry(archive, "META-INF/container.xml", """
                <?xml version="1.0" encoding="utf-8"?>
                <container xmlns="urn:oasis:names:tc:opendocument:xmlns:container" version="1.0">
                  <rootfiles />
                </container>
                """);
        });
        var adapter = new EpubMetadataAdapter();

        var result = await adapter.ReadAsync(path, EbookFormat.Epub, default);

        result.Metadata.Title.Should().Be("Malformed");
        result.Metadata.Authors.Should().Equal("Unknown");
        result.Warning.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Cbz_adapter_chooses_the_alphabetically_first_supported_image()
    {
        byte[] alphaBytes = [0x11, 0x22];
        byte[] betaBytes = [0x33, 0x44];
        var path = CreateArchive("The Hobbit - J.R.R. Tolkien.cbz", archive =>
        {
            AddBinaryEntry(archive, "images/Zeta.png", betaBytes);
            AddBinaryEntry(archive, "images/Alpha.jpeg", alphaBytes);
            AddTextEntry(archive, "notes.txt", "ignored");
        });
        var adapter = new CbzMetadataAdapter();

        var result = await adapter.ReadAsync(path, EbookFormat.Cbz, default);

        result.Metadata.Title.Should().Be("The Hobbit");
        result.Metadata.Authors.Should().Equal("J.R.R. Tolkien");
        result.Metadata.CoverBytes.Should().Equal(alphaBytes);
    }

    [Fact]
    public void Metadata_resolver_prefers_specific_adapters_before_fallback()
    {
        var fallback = new FallbackMetadataAdapter();
        var epub = new EpubMetadataAdapter();
        var cbz = new CbzMetadataAdapter();
        var resolver = new MetadataAdapterResolver([fallback, epub, cbz]);

        resolver.Resolve(EbookFormat.Epub).Should().BeSameAs(epub);
        resolver.Resolve(EbookFormat.Cbz).Should().BeSameAs(cbz);
        resolver.Resolve(EbookFormat.Pdf).Should().BeSameAs(fallback);
    }

    public void Dispose() => temporaryDirectory.Dispose();

    private string WriteTextFile(string relativePath)
    {
        var path = Path.Combine(temporaryDirectory.DirectoryPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "placeholder");
        return path;
    }

    private string WriteBytesFile(string relativePath, byte[] bytes)
    {
        var path = Path.Combine(temporaryDirectory.DirectoryPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private string CreateArchive(string relativePath, Action<ZipArchive> writeEntries)
    {
        var path = Path.Combine(temporaryDirectory.DirectoryPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var stream = File.Create(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false);
        writeEntries(archive);
        return path;
    }

    private static void AddTextEntry(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName);
        using var stream = new StreamWriter(entry.Open(), Encoding.UTF8);
        stream.Write(content);
    }

    private static void AddBinaryEntry(ZipArchive archive, string entryName, byte[] content)
    {
        var entry = archive.CreateEntry(entryName);
        using var stream = entry.Open();
        stream.Write(content);
    }
}

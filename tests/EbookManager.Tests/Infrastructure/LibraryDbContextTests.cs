using EbookManager.Domain.Books;
using EbookManager.Domain.Importing;
using EbookManager.Infrastructure.Persistence;
using EbookManager.Infrastructure.Persistence.Entities;
using EbookManager.Infrastructure.Persistence.Repositories;
using EbookManager.Tests.TestSupport;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Tests.Infrastructure;

public sealed class LibraryDbContextTests
{
    [Fact]
    public async Task Migration_and_repository_round_trip_full_metadata_to_library_database()
    {
        using var library = new TemporaryLibrary();
        var libraryPath = library.DirectoryPath;
        var factory = new LibraryDbContextFactory();
        await using (var context = factory.Create(libraryPath))
        {
            await context.Database.MigrateAsync();
        }

        var repository = new EfBookRepository(factory, libraryPath);
        var book = CreateBook(
            title: "Test Book",
            authors: ["First Author", "Second Author"],
            tags: ["Reference", "SQLite"],
            publicationDate: new DateOnly(2026, 6, 1),
            coverBytes: [1, 2, 3]);
        var file = CreateFile(book.Id, sha256: "ABC123");

        await repository.AddAsync(book, file, default);

        File.Exists(Path.Combine(libraryPath, "library.db")).Should().BeTrue();
        (await repository.ListAsync(default)).Should().ContainSingle().Which.Should().Be(book);
        (await repository.GetAsync(book.Id, default)).Should().Be(book);
        (await repository.HasHashAsync("ABC123", default)).Should().BeTrue();
        (await repository.HasNormalizedTitleAndAuthorAsync(
            "  TEST BOOK ",
            [" first author ", "SECOND AUTHOR"],
            default)).Should().BeTrue();

        var firstRead = await repository.GetAsync(book.Id, default);
        firstRead!.Metadata.CoverBytes![0] = 99;
        (await repository.GetAsync(book.Id, default))!.Metadata.CoverBytes.Should().Equal(1, 2, 3);

        await using var verificationContext = factory.Create(libraryPath);
        (await verificationContext.Authors.OrderBy(x => x.Name).Select(x => x.Name).ToListAsync())
            .Should().Equal("First Author", "Second Author");
        (await verificationContext.Tags.OrderBy(x => x.Name).Select(x => x.Name).ToListAsync())
            .Should().Equal("Reference", "SQLite");

        var connection = (SqliteConnection)verificationContext.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT PublicationDate, ReadingStatus, Format, WriteBackStatus
            FROM Books
            JOIN BookFiles ON Books.Id = BookFiles.BookId
            WHERE Books.Id = $id
            """;
        command.Parameters.AddWithValue("$id", book.Id);
        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        reader.GetString(0).Should().Be("2026-06-01");
        reader.GetString(1).Should().Be(nameof(ReadingStatus.Reading));
        reader.GetString(2).Should().Be(nameof(EbookFormat.Epub));
        reader.GetString(3).Should().Be(nameof(MetadataWriteBackStatus.NotAttempted));
    }

    [Fact]
    public async Task Repository_preserves_author_order_updates_metadata_and_deletes_books()
    {
        using var library = new TemporaryLibrary();
        var libraryPath = library.DirectoryPath;
        var factory = await CreateMigratedFactoryAsync(libraryPath);
        var repository = new EfBookRepository(factory, libraryPath);
        var book = CreateBook("Ordered Authors", ["Second", "First"], ["Old"]);

        await repository.AddAsync(book, CreateFile(book.Id), default);

        (await repository.GetAsync(book.Id, default))!.Metadata.Authors.Should().Equal("Second", "First");

        var updated = book with
        {
            Metadata = new BookMetadata(
                "Updated",
                ["Third", "Second", "First"],
                Tags: ["New"]),
            ReadingStatus = ReadingStatus.Read,
            UpdatedUtc = book.UpdatedUtc.AddMinutes(1)
        };
        await repository.UpdateAsync(updated, default);

        var reloaded = await repository.GetAsync(book.Id, default);
        reloaded.Should().Be(updated);
        reloaded!.Metadata.Authors.Should().Equal("Third", "Second", "First");
        reloaded.Metadata.Tags.Should().Equal("New");

        await repository.DeleteAsync(book.Id, default);

        (await repository.ListAsync(default)).Should().BeEmpty();
        (await repository.GetAsync(book.Id, default)).Should().BeNull();
    }

    [Fact]
    public async Task Unique_file_hash_and_import_entities_are_enforced_by_relational_schema()
    {
        using var library = new TemporaryLibrary();
        var libraryPath = library.DirectoryPath;
        var factory = await CreateMigratedFactoryAsync(libraryPath);
        var repository = new EfBookRepository(factory, libraryPath);
        var firstBook = CreateBook("First", ["Author"]);
        var secondBook = CreateBook("Second", ["Author"]);

        await repository.AddAsync(firstBook, CreateFile(firstBook.Id, sha256: "SAME"), default);
        var addDuplicate = () => repository.AddAsync(secondBook, CreateFile(secondBook.Id, sha256: "SAME"), default);

        await addDuplicate.Should().ThrowAsync<DbUpdateException>();

        await using var context = factory.Create(libraryPath);
        var importRun = new ImportRunEntity
        {
            Id = Guid.NewGuid(),
            StartedUtc = DateTimeOffset.UtcNow,
            CompletedUtc = DateTimeOffset.UtcNow
        };
        importRun.Items.Add(new ImportItemEntity
        {
            Id = Guid.NewGuid(),
            SourcePath = "source.epub",
            Outcome = ImportOutcome.Added,
            Message = "Imported",
            BookId = firstBook.Id
        });
        context.ImportRuns.Add(importRun);
        await context.SaveChangesAsync();

        (await context.ImportItems.SingleAsync()).Outcome.Should().Be(ImportOutcome.Added);
    }

    [Fact]
    public void Design_time_factory_uses_a_temporary_sqlite_database()
    {
        var factory = new DesignTimeLibraryDbContextFactory();

        using var context = factory.CreateDbContext([]);

        context.Database.GetDbConnection().DataSource.Should().StartWith(Path.GetTempPath());
        context.Database.GetDbConnection().DataSource.Should().EndWith("library.db");
    }

    private static async Task<LibraryDbContextFactory> CreateMigratedFactoryAsync(string libraryPath)
    {
        var factory = new LibraryDbContextFactory();
        await using var context = factory.Create(libraryPath);
        await context.Database.MigrateAsync();
        return factory;
    }

    private static Book CreateBook(
        string title,
        IReadOnlyList<string> authors,
        IReadOnlyList<string>? tags = null,
        DateOnly? publicationDate = null,
        byte[]? coverBytes = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata(
                title,
                authors,
                Description: "Description",
                Language: "en",
                Publisher: "Publisher",
                PublicationDate: publicationDate,
                Tags: tags,
                Series: "Series",
                SeriesNumber: 1.5m,
                Isbn: "9780000000000",
                CoverBytes: coverBytes),
            ReadingStatus.Reading,
            "books/cover.jpg",
            now,
            now);
    }

    private static BookFile CreateFile(Guid bookId, string sha256 = "HASH") =>
        new(
            Guid.NewGuid(),
            bookId,
            EbookFormat.Epub,
            $"books/{bookId:N}/book.epub",
            sha256,
            123,
            MetadataWriteBackStatus.NotAttempted,
            null);

    private sealed class TemporaryLibrary : IDisposable
    {
        private readonly TemporaryDirectory _temporaryDirectory = new();

        public TemporaryLibrary()
        {
            DirectoryPath = _temporaryDirectory.CreateSubdirectory("ELibrary").FullName;
        }

        public string DirectoryPath { get; }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            _temporaryDirectory.Dispose();
        }
    }
}

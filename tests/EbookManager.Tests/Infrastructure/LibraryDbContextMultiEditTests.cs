using EbookManager.Application.Books;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using EbookManager.Domain.CustomMetadata;
using Microsoft.EntityFrameworkCore;
using EbookManager.Infrastructure.Files;
using EbookManager.Infrastructure.Metadata;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Fact]
    public async Task MultiEdit_returns_actual_stored_authors_and_writes_real_metadata_sidecars()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var original = CreateBook("Original", ["Old"]);
        var existing = CreateBook("Other", ["Canonical Name"]);
        var file = CreateFile(original.Id, Hash(1));
        await books.AddAsync(original, file, default);
        await books.AddAsync(existing, CreateFile(existing.Id, Hash(2)), default);
        var sidecars = new JsonMetadataSidecarStore();
        var service = new MetadataMultiEditService(books, new BookService(books,
            new ManagedLibraryFileStore(library.DirectoryPath), new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));

        var result = (await service.ApplyAsync([original.Id], new(true, "CANONICAL NAME", false, "", false, "", false, "", false, ReadingStatus.Unread), default)).Single();

        var persisted = (await books.GetAsync(original.Id, default))!;
        result.Book!.Metadata.Authors.Should().Equal(persisted.Metadata.Authors);
        (await sidecars.TryReadAsync(Path.Combine(library.DirectoryPath, file.RelativePath), default))!.Authors.Should().Equal(persisted.Metadata.Authors);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MultiEdit_reports_partial_custom_storage_without_undoing_saved_fields(bool updateStandard)
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var custom = new EfCustomMetadataRepository(factory, library.DirectoryPath);
        var book = CreateBook("Book", ["Original"]);
        await books.AddAsync(book, CreateFile(book.Id), default);
        var first = await custom.AddDefinitionAsync("Working", CustomMetadataFieldType.Text, default);
        var broken = await custom.AddDefinitionAsync("Broken", CustomMetadataFieldType.Text, default);
        await using (var context = factory.Create(library.DirectoryPath))
            await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER DenyCustom BEFORE INSERT ON CustomMetadataValues WHEN NEW.FieldId = (SELECT Id FROM CustomMetadataFields WHERE Name = 'Broken') BEGIN SELECT RAISE(ABORT, 'Simulated custom failure'); END;");
        var service = new MetadataMultiEditService(books, null, custom);
        var request = new MetadataMultiEditRequest(updateStandard, "New", false, "", false, "", false, "", false, ReadingStatus.Unread,
            CustomFields: [new(first.Id, first.Name, new(Guid.Empty, first.Id, TextValue: "Saved")), new(broken.Id, broken.Name, new(Guid.Empty, broken.Id, TextValue: "Not saved"))]);

        var item = (await service.ApplyAsync([book.Id], request, default)).Single();

        item.Status.Should().Be(MultiEditStatus.Partial);
        item.Failure.Should().Be(MultiEditFailure.Custom);
        item.CustomFields.Should().Equal("Broken");
        (await custom.GetValuesAsync(book.Id, default)).Should().ContainSingle().Which.TextValue.Should().Be("Saved");
        (await books.GetAsync(book.Id, default))!.Metadata.Authors.Should().Equal(updateStandard ? "New" : "Original");
    }

    [Fact]
    public async Task MultiEdit_custom_only_unchanged_and_cancelled_requests_do_not_reapply_changes()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var custom = new EfCustomMetadataRepository(factory, library.DirectoryPath);
        var book = CreateBook("Book", ["Original"]);
        await books.AddAsync(book, CreateFile(book.Id), default);
        var field = await custom.AddDefinitionAsync("Note", CustomMetadataFieldType.Text, default);
        var request = new MetadataMultiEditRequest(false, "", false, "", false, "", false, "", false, ReadingStatus.Unread,
            CustomFields: [new(field.Id, field.Name, new(Guid.Empty, field.Id, TextValue: "Saved"))]);
        var service = new MetadataMultiEditService(books, null, custom);

        (await service.ApplyAsync([book.Id, book.Id], request, default)).Should().ContainSingle().Which.Status.Should().Be(MultiEditStatus.Updated);
        (await service.ApplyAsync([book.Id], request, default)).Single().Status.Should().Be(MultiEditStatus.Unchanged);
        (await new MetadataMultiEditService(books, null).ApplyAsync([book.Id], request, default)).Single().Failure.Should().Be(MultiEditFailure.CustomUnavailable);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        (await service.ApplyAsync([book.Id], request, cancelled.Token)).Single().Status.Should().Be(MultiEditStatus.NotProcessed);
        (await books.GetAsync(book.Id, default))!.Metadata.Authors.Should().Equal("Original");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MultiEdit_continues_after_conflict_and_preserves_database_success_on_sidecar_failure(bool failFiles)
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var conflicting = CreateBook("Same", ["Original"]);
        var existing = CreateBook("Same", ["New"]);
        var success = CreateBook("Other", ["Original"]);
        var hashIndex = 0;
        foreach (var book in new[] { conflicting, existing, success })
            await books.AddAsync(book, CreateFile(book.Id, Hash(++hashIndex)), default);
        var sidecars = new AuthorRenameSidecars(failFiles);
        var service = new MetadataMultiEditService(books, new BookService(books,
            new ManagedLibraryFileStore(library.DirectoryPath), new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));

        var results = await service.ApplyAsync([conflicting.Id, success.Id, Guid.NewGuid()],
            new(true, "New", false, "", false, "", false, "", false, ReadingStatus.Unread), default);

        results[0].Failure.Should().Be(MultiEditFailure.Duplicate);
        results[1].Status.Should().Be(failFiles ? MultiEditStatus.FileWarning : MultiEditStatus.Updated);
        results[2].Status.Should().Be(MultiEditStatus.NotFound);
        (await books.GetAsync(conflicting.Id, default))!.Metadata.Authors.Should().Equal("Original");
        (await books.GetAsync(success.Id, default))!.Metadata.Authors.Should().Equal("New");
        sidecars.WrittenAuthors.Should().Equal("New");
    }
}

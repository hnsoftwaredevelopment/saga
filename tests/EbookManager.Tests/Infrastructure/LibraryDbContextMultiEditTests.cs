using EbookManager.Application.Books;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using EbookManager.Infrastructure.Files;
using EbookManager.Infrastructure.Metadata;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
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

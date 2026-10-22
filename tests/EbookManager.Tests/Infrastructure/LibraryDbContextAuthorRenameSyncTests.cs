using EbookManager.Application.Authors;
using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Authors;
using EbookManager.Domain.Books;
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
    public async Task Author_rename_synchronizes_sidecars_after_commit_and_reports_file_errors_without_undoing_rename(bool failWrite)
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var book = CreateBook("Book", ["Original"]);
        await books.AddAsync(book, CreateFile(book.Id), default);
        var repository = new EfAuthorProfileRepository(factory, library.DirectoryPath);
        var author = (await repository.SearchAsync("Original", 0, 200, default)).Authors.Single();
        var sidecars = new AuthorRenameSidecars(failWrite);
        var service = new AuthorRenameService(repository, new BookService(books,
            new ManagedLibraryFileStore(library.DirectoryPath), new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));

        var preview = (await service.PreviewAsync(author.Id, default))!;
        var result = await service.RenameAsync(preview, "Renamed", default);

        result.Change.Status.Should().Be(AuthorRenameStatus.Renamed);
        result.Warnings.Count.Should().Be(failWrite ? 1 : 0);
        sidecars.WrittenAuthors.Should().Equal("Renamed");
        (await books.GetAsync(book.Id, default))!.Metadata.Authors.Should().Equal("Renamed");
        (await repository.GetAsync(author.Id, default))!.Name.Should().Be("Renamed");
    }

    private sealed class AuthorRenameSidecars(bool failWrite) : IMetadataSidecarStore
    {
        public IReadOnlyList<string> WrittenAuthors { get; private set; } = [];
        public Task<BookMetadata?> TryReadAsync(string path, CancellationToken token) => Task.FromResult<BookMetadata?>(null);
        public Task WriteAsync(string path, BookMetadata metadata, CancellationToken token)
        {
            WrittenAuthors = metadata.Authors;
            if (failWrite) throw new IOException("Simulated sidecar failure");
            return Task.CompletedTask;
        }
    }
}

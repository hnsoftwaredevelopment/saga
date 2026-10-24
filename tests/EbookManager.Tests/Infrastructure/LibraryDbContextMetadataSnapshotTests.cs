using EbookManager.Infrastructure.Persistence.Entities;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Fact]
    public async Task Metadata_snapshot_reads_requested_books_across_chunks_without_cover_blobs()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var repository = new EfBookRepository(factory, library.DirectoryPath);
        var original = CreateBook("With metadata", ["Author", "Coauthor"], tags: ["Tag"], coverBytes: [1, 2, 3]);
        await repository.AddAsync(original, CreateFile(original.Id), default);
        var ids = new List<Guid> { original.Id };
        await using (var context = factory.Create(library.DirectoryPath))
        {
            for (var index = 0; index < 500; index++)
            {
                var id = Guid.NewGuid();
                ids.Add(id);
                context.Books.Add(new BookEntity { Id = id, Title = $"Book {index}", NormalizedTitle = $"book {index}", DuplicateKey = $"snapshot-{index}", CoverBytes = [4, 5, 6] });
            }
            await context.SaveChangesAsync();
        }
        ids.Add(Guid.NewGuid()); // A deleted/missing book must not prevent refreshing the rest.
        ids.Add(original.Id);

        var snapshot = await repository.ListMetadataAsync(ids, default);

        snapshot.Should().HaveCount(501).And.OnlyHaveUniqueItems(book => book.Id);
        snapshot.Should().OnlyContain(book => book.Metadata.CoverBytes == null);
        var first = snapshot.Single(book => book.Id == original.Id);
        first.Metadata.Authors.Should().Equal("Author", "Coauthor");
        first.Metadata.Tags.Should().Equal("Tag");
        first.CoverRelativePath.Should().Be(original.CoverRelativePath);
        first.Formats.Should().Equal(original.Formats);
        (await repository.GetAsync(original.Id, default))!.Metadata.CoverBytes.Should().Equal(1, 2, 3);
        (await repository.ListMetadataAsync([], default)).Should().BeEmpty();
    }
}

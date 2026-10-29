using EbookManager.Domain.Tags;
using EbookManager.Infrastructure.Persistence;
using EbookManager.Infrastructure.Persistence.Entities;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Fact]
    public async Task Tag_reads_use_identity_and_page_titles_without_loading_other_tags_or_covers()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var id = await SeedTagBooksAsync(factory, library.DirectoryPath, 210);
        await using (var context = factory.Create(library.DirectoryPath))
        {
            context.Tags.Add(new TagEntity { Id = Guid.NewGuid(), Name = "Unused", NormalizedName = "unused" });
            await context.SaveChangesAsync();
        }
        var repository = new EfTagRepository(factory, library.DirectoryPath);
        var tags = await repository.SearchAsync(" TARGET ", 0, 200, default);
        tags.TotalCount.Should().Be(1);
        tags.Tags.Single().Should().Be(new TagSummary(id, "Target", 210));
        (await repository.SearchAsync("", 0, 200, default)).Tags.Should().NotContain(tag => tag.Name == "Unused");
        var first = await repository.ListBooksAsync(id, 0, 200, default);
        var last = await repository.ListBooksAsync(id, 200, 200, default);
        first.TotalCount.Should().Be(210);
        first.Books.Concat(last.Books).Select(book => book.Title).Should().Equal(Enumerable.Range(0, 210).Select(index => $"Book {index:D3}"));
        first.Books.Should().OnlyContain(book => book.Authors == "Author");
        (await repository.ListBooksAsync(Guid.NewGuid(), 0, 200, default)).Books.Should().BeEmpty();
    }

    [Fact]
    public async Task Tag_rename_preserves_identity_other_tags_order_and_unrelated_book_data()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var book = CreateBook("Book", ["Primary", "Coauthor"], ["Before", "Target", "After"], coverBytes: [1, 2, 3]);
        var other = CreateBook("Other", ["Other Author"], ["Before"]);
        await books.AddAsync(book, CreateFile(book.Id, Hash(1)), default);
        await books.AddAsync(other, CreateFile(other.Id, Hash(2)), default);
        await using var context = factory.Create(library.DirectoryPath);
        var tagId = await context.Tags.Where(tag => tag.Name == "Target").Select(tag => tag.Id).SingleAsync();
        var original = await books.GetAsync(book.Id, default);
        var otherBefore = await books.GetAsync(other.Id, default);
        var links = await context.BookTags.AsNoTracking().OrderBy(link => link.Order).Select(link => new { link.BookId, link.TagId, link.Order }).ToArrayAsync();
        var repository = new EfTagRepository(factory, library.DirectoryPath);
        var result = await repository.RenameAsync(await repository.PreviewRenameAsync(tagId, default), " New name ", default);
        result.Status.Should().Be(TagRenameStatus.Renamed);
        result.TagId.Should().Be(tagId);
        var updated = (await books.GetAsync(book.Id, default))!;
        updated.Metadata.Tags.Should().Equal("Before", "New name", "After");
        updated.Metadata.Authors.Should().Equal(original!.Metadata.Authors);
        updated.Metadata.Title.Should().Be(original.Metadata.Title);
        updated.Metadata.Series.Should().Be(original.Metadata.Series);
        updated.Metadata.SeriesNumber.Should().Be(original.Metadata.SeriesNumber);
        updated.Metadata.Isbn.Should().Be(original.Metadata.Isbn);
        updated.Metadata.CoverBytes.Should().Equal(original.Metadata.CoverBytes!);
        (await books.GetAsync(other.Id, default)).Should().BeEquivalentTo(otherBefore);
        (await context.BookTags.AsNoTracking().OrderBy(link => link.Order).Select(link => new { link.BookId, link.TagId, link.Order }).ToArrayAsync())
            .Should().BeEquivalentTo(links, options => options.WithStrictOrdering());
        (await context.Tags.AsNoTracking().SingleAsync(tag => tag.Id == tagId)).NormalizedName.Should().Be("new name");
    }

    [Fact]
    public async Task Tag_rename_changes_all_pages_and_rolls_back_database_failure()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var id = await SeedTagBooksAsync(factory, library.DirectoryPath, 510);
        var repository = new EfTagRepository(factory, library.DirectoryPath);
        var preview = await repository.PreviewRenameAsync(id, default);
        preview.Books.Should().HaveCount(510);
        await using (var context = factory.Create(library.DirectoryPath))
            await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_tag_timestamp BEFORE UPDATE OF UpdatedUtc ON Books WHEN OLD.Title = 'Book 509' BEGIN SELECT RAISE(ABORT, 'test failure'); END;");
        await FluentActions.Awaiting(() => repository.RenameAsync(preview, "New", default)).Should().ThrowAsync<Microsoft.Data.Sqlite.SqliteException>();
        await using (var context = factory.Create(library.DirectoryPath))
        {
            (await context.Tags.AsNoTracking().SingleAsync(tag => tag.Id == id)).Name.Should().Be("Target");
            (await context.Books.AsNoTracking().Select(book => book.UpdatedUtc).ToListAsync()).Should().OnlyContain(date => date == DateTimeOffset.UnixEpoch);
            await context.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_tag_timestamp;");
        }
        (await repository.RenameAsync(preview, "New", default)).Books.Should().HaveCount(510);
        await using var verify = factory.Create(library.DirectoryPath);
        (await verify.Books.AsNoTracking().Select(book => book.UpdatedUtc).ToListAsync()).Should().OnlyContain(date => date > DateTimeOffset.UnixEpoch);
        (await verify.BookTags.CountAsync(link => link.TagId == id)).Should().Be(510);
    }

    [Fact]
    public async Task Tag_rename_rejects_existing_name_stale_links_and_stale_name_but_allows_case_correction()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var id = await SeedTagBooksAsync(factory, library.DirectoryPath, 2);
        await using (var context = factory.Create(library.DirectoryPath))
        {
            context.Tags.Add(new TagEntity { Id = Guid.NewGuid(), Name = "Unused", NormalizedName = "unused" });
            await context.SaveChangesAsync();
        }
        var repository = new EfTagRepository(factory, library.DirectoryPath);
        var preview = await repository.PreviewRenameAsync(id, default);
        (await repository.RenameAsync(preview, " unused ", default)).Status.Should().Be(TagRenameStatus.NameExists);
        (await repository.RenameAsync(preview, "Target", default)).Status.Should().Be(TagRenameStatus.Unchanged);
        (await repository.RenameAsync(preview, "target", default)).Status.Should().Be(TagRenameStatus.Renamed);
        (await repository.RenameAsync(preview, "New", default)).Status.Should().Be(TagRenameStatus.Conflict);
        var fresh = await repository.PreviewRenameAsync(id, default);
        await using (var context = factory.Create(library.DirectoryPath))
        {
            var link = await context.BookTags.FirstAsync();
            context.BookTags.Remove(link);
            await context.SaveChangesAsync();
        }
        (await repository.RenameAsync(fresh, "New", default)).Status.Should().Be(TagRenameStatus.Conflict);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("One;Two")]
    [InlineData("One\nTwo")]
    public async Task Tag_rename_refuses_unusable_names(string name)
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var id = await SeedTagBooksAsync(factory, library.DirectoryPath, 1);
        var repository = new EfTagRepository(factory, library.DirectoryPath);
        (await repository.RenameAsync(await repository.PreviewRenameAsync(id, default), name, default)).Status.Should().Be(TagRenameStatus.InvalidName);
    }

    private static async Task<Guid> SeedTagBooksAsync(LibraryDbContextFactory factory, string path, int count)
    {
        await using var context = factory.Create(path);
        var tag = new TagEntity { Id = Guid.NewGuid(), Name = "Target", NormalizedName = "target" };
        var author = new AuthorEntity { Id = Guid.NewGuid(), Name = "Author", NormalizedName = "author" };
        for (var index = 0; index < count; index++) context.Books.Add(new BookEntity
        {
            Id = Guid.NewGuid(), Title = $"Book {index:D3}", NormalizedTitle = $"book {index:D3}", DuplicateKey = $"tag-{index}",
            UpdatedUtc = DateTimeOffset.UnixEpoch, CoverBytes = [1, 2, 3],
            BookTags = [new() { Tag = tag, Order = 0 }], BookAuthors = [new() { Author = author, Order = 0 }]
        });
        await context.SaveChangesAsync();
        return tag.Id;
    }
}

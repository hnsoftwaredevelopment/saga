using EbookManager.Domain.Tags;
using EbookManager.Infrastructure.Persistence.Entities;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Global_tag_rename_reuses_existing_target_without_duplicate_links_and_preserves_order(bool failUpdate)
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var first = CreateBook("First", ["Author"], ["Before", "Wrong", "After"]);
        var both = CreateBook("Both", ["Author"], ["Before", "Wrong", "Middle", "Roman", "After"]);
        var untouched = CreateBook("Untouched", ["Author"], ["Roman"]);
        await books.AddAsync(first, CreateFile(first.Id, Hash(1)), default);
        await books.AddAsync(both, CreateFile(both.Id, Hash(2)), default);
        await books.AddAsync(untouched, CreateFile(untouched.Id, Hash(3)), default);
        var before = await books.GetAsync(untouched.Id, default);
        await using var context = factory.Create(library.DirectoryPath);
        var sourceId = await context.Tags.Where(tag => tag.Name == "Wrong").Select(tag => tag.Id).SingleAsync();
        var targetId = await context.Tags.Where(tag => tag.Name == "Roman").Select(tag => tag.Id).SingleAsync();
        var repository = new EfTagRepository(factory, library.DirectoryPath);
        var preview = await repository.PreviewRenameAsync(sourceId, default);
        if (failUpdate)
        {
            await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_merge BEFORE UPDATE OF UpdatedUtc ON Books BEGIN SELECT RAISE(ABORT, 'test failure'); END;");
            await FluentActions.Awaiting(() => repository.RenameAsync(preview, " roman ", default)).Should().ThrowAsync<Microsoft.Data.Sqlite.SqliteException>();
            (await books.GetAsync(first.Id, default))!.Metadata.Tags.Should().Equal("Before", "Wrong", "After");
            (await books.GetAsync(both.Id, default))!.Metadata.Tags.Should().Equal("Before", "Wrong", "Middle", "Roman", "After");
            (await context.Tags.AnyAsync(tag => tag.Id == sourceId)).Should().BeTrue();
            await context.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_merge;");
        }
        var result = await repository.RenameAsync(preview, " roman ", default);
        result.Status.Should().Be(TagRenameStatus.Renamed);
        result.TagId.Should().Be(targetId);
        result.Name.Should().Be("Roman");
        result.Books.Select(book => book.Id).Should().BeEquivalentTo([first.Id, both.Id]);
        (await books.GetAsync(first.Id, default))!.Metadata.Tags.Should().Equal("Before", "Roman", "After");
        (await books.GetAsync(both.Id, default))!.Metadata.Tags.Should().Equal("Before", "Middle", "Roman", "After");
        (await books.GetAsync(untouched.Id, default)).Should().BeEquivalentTo(before);
        (await context.Tags.AnyAsync(tag => tag.Id == sourceId)).Should().BeFalse();
        (await repository.SearchAsync("Roman", 0, 200, default)).Tags.Single().BookCount.Should().Be(3);
        (await repository.ListBooksAsync(sourceId, 0, 200, default)).TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Global_tag_merge_updates_all_books_across_pages_and_reuses_an_unused_target()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var sourceId = await SeedTagBooksAsync(factory, library.DirectoryPath, 510);
        var targetId = Guid.NewGuid();
        await using var context = factory.Create(library.DirectoryPath);
        context.Tags.Add(new TagEntity { Id = targetId, Name = "Roman", NormalizedName = "roman" });
        await context.SaveChangesAsync();
        var repository = new EfTagRepository(factory, library.DirectoryPath);
        var result = await repository.RenameAsync(await repository.PreviewRenameAsync(sourceId, default), "Roman", default);
        result.TagId.Should().Be(targetId);
        result.Books.Should().HaveCount(510);
        (await context.BookTags.CountAsync(link => link.TagId == targetId)).Should().Be(510);
        (await context.Tags.AnyAsync(tag => tag.Id == sourceId)).Should().BeFalse();
    }
}

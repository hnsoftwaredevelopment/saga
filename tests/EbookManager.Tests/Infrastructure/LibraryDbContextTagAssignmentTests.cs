using EbookManager.Domain.Tags;
using EbookManager.Infrastructure.Persistence.Entities;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Fact]
    public async Task Tag_add_preserves_existing_links_and_returns_only_changed_books()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var first = CreateBook("First", ["Author"], ["Before", "Source", "After"]);
        var second = CreateBook("Second", ["Author"], ["Existing", "Source", "New"]);
        var untouched = CreateBook("Untouched", ["Author"], ["Source"]);
        var all = new[] { first, second, untouched };
        for (var index = 0; index < all.Length; index++) await books.AddAsync(all[index], CreateFile(all[index].Id, Hash(index + 1)), default);
        var secondBefore = await books.GetAsync(second.Id, default);
        var repository = new EfTagRepository(factory, library.DirectoryPath);
        var source = (await repository.SearchAsync("Source", 0, 200, default)).Tags.Single();
        var preview = await repository.PreviewAssignmentsAsync(source.Id, [first.Id, second.Id], default);
        var request = new TagAssignmentRequest(TagAssignmentOperation.Add) { TargetNames = [" existing ", " new ", "SOURCE", "Existing"] };

        var result = await repository.ApplyAssignmentsAsync(preview, request, default);

        result.Status.Should().Be(TagAssignmentStatus.Applied);
        result.Books.Select(book => book.Id).Should().Equal(first.Id);
        (await books.GetAsync(first.Id, default))!.Metadata.Tags.Should().Equal("Before", "Source", "After", "Existing", "New");
        (await books.GetAsync(second.Id, default)).Should().BeEquivalentTo(secondBefore);
        (await books.GetAsync(untouched.Id, default))!.Metadata.Tags.Should().Equal("Source");
        (await repository.ListBooksAsync(source.Id, 0, 200, default)).TotalCount.Should().Be(3);
        (await repository.ApplyAssignmentsAsync(preview, request, default)).Status.Should().Be(TagAssignmentStatus.Unchanged);
    }

    [Fact]
    public async Task Tag_add_rolls_back_created_tags_and_links_on_failure()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var id = await SeedTagBooksAsync(factory, library.DirectoryPath, 2);
        var repository = new EfTagRepository(factory, library.DirectoryPath);
        var ids = (await repository.ListBooksAsync(id, 0, 200, default)).Books.Select(book => book.Id).ToArray();
        var preview = await repository.PreviewAssignmentsAsync(id, ids, default);
        var request = new TagAssignmentRequest(TagAssignmentOperation.Add) { TargetNames = ["New", "Another"] };
        await using var context = factory.Create(library.DirectoryPath);
        await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_add BEFORE UPDATE OF UpdatedUtc ON Books BEGIN SELECT RAISE(ABORT, 'test failure'); END;");
        await FluentActions.Awaiting(() => repository.ApplyAssignmentsAsync(preview, request, default)).Should().ThrowAsync<Microsoft.Data.Sqlite.SqliteException>();
        (await context.Tags.CountAsync()).Should().Be(1);
        (await context.BookTags.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Tag_assignment_moves_only_selected_books_and_deduplicates_an_existing_target()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var first = CreateBook("First", ["Author"], ["Before", "Source", "After"]);
        var second = CreateBook("Second", ["Author"], ["Before", "Source", "Middle", "Target", "After"]);
        var untouched = CreateBook("Untouched", ["Author"], ["Source"]);
        var all = new[] { first, second, untouched };
        for (var index = 0; index < all.Length; index++) await books.AddAsync(all[index], CreateFile(all[index].Id, Hash(index + 1)), default);
        var otherBefore = await books.GetAsync(untouched.Id, default);
        var repository = new EfTagRepository(factory, library.DirectoryPath);
        var source = (await repository.SearchAsync("Source", 0, 200, default)).Tags.Single();
        var target = (await repository.SearchAsync("Target", 0, 200, default)).Tags.Single();
        var preview = await repository.PreviewAssignmentsAsync(source.Id, [first.Id, second.Id], default);
        var result = await repository.ApplyAssignmentsAsync(preview, new(TagAssignmentOperation.Replace, " target "), default);
        result.Status.Should().Be(TagAssignmentStatus.Applied);
        result.TargetTagId.Should().Be(target.Id);
        (await books.GetAsync(first.Id, default))!.Metadata.Tags.Should().Equal("Before", "Target", "After");
        (await books.GetAsync(second.Id, default))!.Metadata.Tags.Should().Equal("Before", "Middle", "Target", "After");
        (await books.GetAsync(untouched.Id, default)).Should().BeEquivalentTo(otherBefore);
        (await repository.ListBooksAsync(source.Id, 0, 200, default)).Books.Select(book => book.Id).Should().Equal(untouched.Id);
        (await repository.ListBooksAsync(target.Id, 0, 200, default)).TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Tag_assignment_can_create_a_target_or_remove_the_last_tag_without_deleting_books()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var id = await SeedTagBooksAsync(factory, library.DirectoryPath, 2);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var repository = new EfTagRepository(factory, library.DirectoryPath);
        var ids = (await repository.ListBooksAsync(id, 0, 200, default)).Books.Select(book => book.Id).ToArray();
        var replace = await repository.ApplyAssignmentsAsync(await repository.PreviewAssignmentsAsync(id, [ids[0]], default),
            new(TagAssignmentOperation.Replace, "New"), default);
        replace.Status.Should().Be(TagAssignmentStatus.Applied);
        replace.TargetTagId.Should().NotBe(id);
        (await books.GetAsync(ids[0], default))!.Metadata.Tags.Should().Equal("New");
        var remove = await repository.ApplyAssignmentsAsync(await repository.PreviewAssignmentsAsync(id, [ids[1]], default),
            new(TagAssignmentOperation.Remove), default);
        remove.Status.Should().Be(TagAssignmentStatus.Applied);
        var untagged = await books.GetAsync(ids[1], default);
        untagged.Should().NotBeNull();
        (untagged!.Metadata.Tags ?? []).Should().BeEmpty();
        (await repository.SearchAsync("Target", 0, 200, default)).Tags.Should().BeEmpty();
        (await repository.ListBooksAsync(replace.TargetTagId!.Value, 0, 200, default)).Books.Should().ContainSingle();
    }

    [Theory]
    [InlineData(TagAssignmentOperation.Replace)]
    [InlineData(TagAssignmentOperation.Remove)]
    [InlineData(TagAssignmentOperation.Add)]
    public async Task Tag_assignment_changes_selected_books_across_chunks_and_keeps_the_remainder(TagAssignmentOperation operation)
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var id = await SeedTagBooksAsync(factory, library.DirectoryPath, 510);
        var repository = new EfTagRepository(factory, library.DirectoryPath);
        var all = await repository.PreviewRenameAsync(id, default);
        var selected = all.Books.Take(505).Select(book => book.Id).ToArray();
        var request = new TagAssignmentRequest(operation, "New") { TargetNames = ["New", "Another"] };
        var result = await repository.ApplyAssignmentsAsync(await repository.PreviewAssignmentsAsync(id, selected, default), request, default);
        result.Books.Should().HaveCount(505);
        (await repository.ListBooksAsync(id, 0, 200, default)).TotalCount.Should().Be(operation == TagAssignmentOperation.Add ? 510 : 5);
        if (operation == TagAssignmentOperation.Replace)
            (await repository.ListBooksAsync(result.TargetTagId!.Value, 0, 200, default)).TotalCount.Should().Be(505);
        if (operation == TagAssignmentOperation.Add)
        {
            var targets = (await repository.SearchAsync("", 0, 200, default)).Tags;
            targets.Single(tag => tag.Name == "New").BookCount.Should().Be(505);
            targets.Single(tag => tag.Name == "Another").BookCount.Should().Be(505);
        }
        await using var verify = factory.Create(library.DirectoryPath);
        (await verify.Books.CountAsync()).Should().Be(510);
    }

    [Fact]
    public async Task Tag_assignment_preview_refuses_a_partially_missing_requested_selection()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var id = await SeedTagBooksAsync(factory, library.DirectoryPath, 1);
        var repository = new EfTagRepository(factory, library.DirectoryPath);
        var valid = (await repository.ListBooksAsync(id, 0, 200, default)).Books.Single().Id;
        (await repository.PreviewAssignmentsAsync(id, [valid, Guid.NewGuid()], default)).Books.Should().BeEmpty();
    }

    [Fact]
    public async Task Tag_assignment_rejects_stale_selection_and_rolls_back_new_target_and_links_on_failure()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var id = await SeedTagBooksAsync(factory, library.DirectoryPath, 3);
        var repository = new EfTagRepository(factory, library.DirectoryPath);
        var ids = (await repository.ListBooksAsync(id, 0, 200, default)).Books.Select(book => book.Id).ToArray();
        var preview = await repository.PreviewAssignmentsAsync(id, ids, default);
        await using (var context = factory.Create(library.DirectoryPath))
            await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_tag_assignment BEFORE UPDATE OF UpdatedUtc ON Books BEGIN SELECT RAISE(ABORT, 'test failure'); END;");
        await FluentActions.Awaiting(() => repository.ApplyAssignmentsAsync(preview, new(TagAssignmentOperation.Replace, "New"), default))
            .Should().ThrowAsync<Microsoft.Data.Sqlite.SqliteException>();
        await using (var context = factory.Create(library.DirectoryPath))
        {
            (await context.Tags.CountAsync(tag => tag.NormalizedName == "new")).Should().Be(0);
            (await context.BookTags.CountAsync(link => link.TagId == id)).Should().Be(3);
            await context.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_tag_assignment;");
            await context.BookTags.Where(link => link.BookId == ids[0]).ExecuteDeleteAsync();
        }
        (await repository.ApplyAssignmentsAsync(preview, new(TagAssignmentOperation.Remove), default)).Status.Should().Be(TagAssignmentStatus.Conflict);
        (await repository.ListBooksAsync(id, 0, 200, default)).TotalCount.Should().Be(2);
    }
}

using EbookManager.Domain.Authors;
using EbookManager.Domain.Books;
using EbookManager.Infrastructure.Persistence.Repositories;
using EbookManager.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Fact]
    public async Task Author_rename_includes_books_beyond_visible_page_and_rolls_back_on_failure()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var authorId = Guid.NewGuid();
        await using (var context = factory.Create(library.DirectoryPath))
        {
            var author = new AuthorEntity { Id = authorId, Name = "Original", NormalizedName = "original" };
            context.Authors.Add(author);
            for (var i = 0; i < 205; i++)
            {
                var title = $"Book {i}";
                context.Books.Add(new BookEntity
                {
                    Id = Guid.NewGuid(), Title = title, NormalizedTitle = title.ToLowerInvariant(),
                    DuplicateKey = DuplicateKeyNormalizer.BuildDuplicateKey(title, [author.Name]),
                    BookAuthors = [new() { Author = author, Order = 0 }]
                });
            }
            await context.SaveChangesAsync();
            await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER DenyAuthorRename BEFORE UPDATE OF Name ON Authors BEGIN SELECT RAISE(ABORT, 'Simulated failure'); END;");
        }
        var repository = new EfAuthorProfileRepository(factory, library.DirectoryPath);
        var preview = (await repository.PreviewRenameAsync(authorId, default))!;
        preview.BookIds.Should().HaveCount(205);

        (await repository.RenameAsync(preview, "Renamed", default)).Status.Should().NotBe(AuthorRenameStatus.Renamed);
        await using (var context = factory.Create(library.DirectoryPath))
        {
            var keys = await context.Books.Select(x => x.DuplicateKey).ToListAsync();
            keys.Should().OnlyContain(key => key.Contains("original"));
            (await context.Authors.SingleAsync()).Name.Should().Be("Original");
            await context.Database.ExecuteSqlRawAsync("DROP TRIGGER DenyAuthorRename;");
        }
        (await repository.RenameAsync(preview, "Renamed", default)).BookIds.Should().HaveCount(205);
        await using var verification = factory.Create(library.DirectoryPath);
        (await verification.Books.Select(x => x.DuplicateKey).ToListAsync()).Should().OnlyContain(key => key.Contains("renamed"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" Unknown ")]
    public async Task Author_rename_rejects_invalid_names(string name)
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var repository = new EfAuthorProfileRepository(factory, library.DirectoryPath);
        var original = new AuthorProfile(Guid.NewGuid(), "Original", null, null, null);
        var result = await repository.RenameAsync(new(original, []), name, default);
        result.Status.Should().Be(AuthorRenameStatus.InvalidName);
    }

    [Fact]
    public async Task Author_rename_keeps_identity_profile_coauthors_and_valid_duplicate_keys()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var book = CreateBook("Shared title", ["A.K. Straatsma", "Other Author"]);
        await books.AddAsync(book, CreateFile(book.Id), default);
        await using (var context = factory.Create(library.DirectoryPath))
        {
            var author = await context.Authors.SingleAsync(x => x.Name == "A.K. Straatsma");
            author.Biography = "Keep biography";
            author.WebsiteUrl = "https://example.org";
            author.PhotoRelativePath = "authors/portrait.jpg";
            await context.SaveChangesAsync();
        }
        var repository = new EfAuthorProfileRepository(factory, library.DirectoryPath);
        var summary = (await repository.SearchAsync("Straatsma", 0, 200, default)).Authors.Single();
        var preview = (await repository.PreviewRenameAsync(summary.Id, default))!;

        var result = await repository.RenameAsync(preview, " Anne Klaas Straatsma ", default);

        result.Status.Should().Be(AuthorRenameStatus.Renamed);
        result.BookIds.Should().Equal(book.Id);
        result.Profile.Should().Be(preview.Profile with { Name = "Anne Klaas Straatsma" });
        (await books.GetAsync(book.Id, default))!.Metadata.Authors.Should().Equal("Anne Klaas Straatsma", "Other Author");
        (await books.HasNormalizedTitleAndAuthorAsync("Shared title", ["Anne Klaas Straatsma", "Other Author"], default)).Should().BeTrue();
        (await books.HasNormalizedTitleAndAuthorAsync("Shared title", ["A.K. Straatsma", "Other Author"], default)).Should().BeFalse();
        await using var verification = factory.Create(library.DirectoryPath);
        (await verification.BookAuthors.OrderBy(x => x.Order).FirstAsync()).AuthorId.Should().Be(summary.Id);
        (await verification.Authors.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Author_rename_to_existing_name_is_blocked_without_merging_even_without_books()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using (var context = factory.Create(library.DirectoryPath))
        {
            context.Authors.AddRange(
                new() { Id = Guid.NewGuid(), Name = "A.K. Straatsma", NormalizedName = "a.k. straatsma", Biography = "Source" },
                new() { Id = Guid.NewGuid(), Name = "Anne Klaas Straatsma", NormalizedName = "anne klaas straatsma", Biography = "Target" });
            await context.SaveChangesAsync();
        }
        var repository = new EfAuthorProfileRepository(factory, library.DirectoryPath);
        var source = (await repository.SearchAsync("A.K.", 0, 200, default)).Authors.Single();
        var preview = (await repository.PreviewRenameAsync(source.Id, default))!;

        var result = await repository.RenameAsync(preview, " ANNE KLAAS STRAATSMA ", default);

        result.Status.Should().Be(AuthorRenameStatus.NameExists);
        (await repository.GetAsync(source.Id, default)).Should().Be(preview.Profile);
        (await repository.SearchAsync("", 0, 200, default)).TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Author_rename_rejects_a_changed_book_selection_and_keeps_all_books_unchanged()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var first = CreateBook("First", ["Original"]);
        await books.AddAsync(first, CreateFile(first.Id, Hash('A')), default);
        var repository = new EfAuthorProfileRepository(factory, library.DirectoryPath);
        var author = (await repository.SearchAsync("Original", 0, 200, default)).Authors.Single();
        var preview = (await repository.PreviewRenameAsync(author.Id, default))!;
        var second = CreateBook("Added later", ["Original"]);
        await books.AddAsync(second, CreateFile(second.Id, Hash('B')), default);

        (await repository.RenameAsync(preview, "Renamed", default)).Status.Should().Be(AuthorRenameStatus.Conflict);

        (await books.GetAsync(first.Id, default))!.Metadata.Authors.Should().Equal("Original");
        (await books.GetAsync(second.Id, default))!.Metadata.Authors.Should().Equal("Original");
    }
}

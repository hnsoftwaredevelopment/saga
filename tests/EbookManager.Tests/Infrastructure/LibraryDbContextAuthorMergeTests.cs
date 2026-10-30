using EbookManager.Domain.Authors;
using EbookManager.Domain.Books;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthorMerge_preserves_target_profile_choices_coauthor_positions_and_unrelated_books(bool failUpdate)
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var first = CreateBook("First", ["Before", "J.Nowee", "After"]);
        var both = CreateBook("Both", ["Before", "J. Nowee", "Middle", "Johannes Nowee", "J.Nowee", "After"]);
        var other = CreateBook("Untouched", ["Johannes Nowee"]);
        await books.AddAsync(first, CreateFile(first.Id, Hash(1)), default);
        await books.AddAsync(both, CreateFile(both.Id, Hash(2)), default);
        await books.AddAsync(other, CreateFile(other.Id, Hash(3)), default);
        var otherBefore = await books.GetAsync(other.Id, default);
        var beforeFirst = await books.GetAsync(first.Id, default);
        await using var context = factory.Create(library.DirectoryPath);
        var authors = await context.Authors.Where(author => author.Name.Contains("Nowee")).ToArrayAsync();
        var target = authors.Single(author => author.Name == "Johannes Nowee");
        var source = authors.Single(author => author.Name == "J.Nowee");
        source.Biography = "Source biography";
        target.WebsiteUrl = "https://example.org";
        await context.SaveChangesAsync();
        var repository = new EfAuthorProfileRepository(factory, library.DirectoryPath);
        var preview = (await repository.PreviewMergeAsync(authors.Select(author => author.Id).ToArray(), default))!;
        preview.BookCount.Should().Be(3);
        var selection = new AuthorMergeSelection(target.Id, source.Id, target.Id, null);
        if (failUpdate)
        {
            await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_author_merge BEFORE UPDATE OF DuplicateKey ON Books BEGIN SELECT RAISE(ABORT, 'test failure'); END;");
            var failed = await repository.MergeAsync(preview, selection, null, default);
            failed.Status.Should().NotBe(AuthorMergeStatus.Merged);
            (await books.GetAsync(first.Id, default)).Should().BeEquivalentTo(beforeFirst);
            (await context.Authors.AsNoTracking().CountAsync(author => author.Name.Contains("Nowee"))).Should().Be(3);
            await context.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_author_merge;");
        }
        var result = await repository.MergeAsync(preview, selection, null, default);
        result.Status.Should().Be(AuthorMergeStatus.Merged);
        result.Profile.Should().Be(new AuthorProfile(target.Id, target.Name, source.Biography, target.WebsiteUrl, null));
        result.Books.Select(book => book.Id).Should().BeEquivalentTo([first.Id, both.Id]);
        (await books.GetAsync(first.Id, default))!.Metadata.Authors.Should().Equal("Before", "Johannes Nowee", "After");
        (await books.GetAsync(both.Id, default))!.Metadata.Authors.Should().Equal("Before", "Middle", "Johannes Nowee", "After");
        (await books.GetAsync(other.Id, default)).Should().BeEquivalentTo(otherBefore);
        (await context.Authors.AsNoTracking().CountAsync(author => author.Name.Contains("Nowee"))).Should().Be(1);
        var saved = await context.Books.AsNoTracking().SingleAsync(book => book.Id == first.Id);
        saved.DuplicateKey.Should().Be(DuplicateKeyNormalizer.BuildDuplicateKey("First", ["Before", "Johannes Nowee", "After"]));
    }
}

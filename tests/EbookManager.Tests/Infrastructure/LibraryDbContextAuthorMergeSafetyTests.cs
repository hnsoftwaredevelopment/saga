using EbookManager.Domain.Authors;
using EbookManager.Domain.Books;
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
    public async Task AuthorMerge_rejects_colliding_books_without_changing_authors_or_links(bool internalCollision)
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        foreach (var name in new[] { "Source", internalCollision ? "Second" : "Target" })
        {
            var book = CreateBook("Same title", [name]);
            await books.AddAsync(book, CreateFile(book.Id, Hash(name == "Source" ? 1 : 2)), default);
        }
        await using var context = factory.Create(library.DirectoryPath);
        if (internalCollision) { context.Authors.Add(new() { Id = Guid.NewGuid(), Name = "Target", NormalizedName = "target" }); await context.SaveChangesAsync(); }
        var ids = await context.Authors.Select(author => author.Id).ToArrayAsync();
        var target = await context.Authors.SingleAsync(author => author.Name == "Target");
        var repository = new EfAuthorProfileRepository(factory, library.DirectoryPath);
        var preview = (await repository.PreviewMergeAsync(ids, default))!;
        (await repository.MergeAsync(preview, new(target.Id, null, null, null), null, default)).Status.Should().Be(AuthorMergeStatus.DuplicateBook);
        (await context.Authors.AsNoTracking().CountAsync()).Should().Be(ids.Length);
        (await context.BookAuthors.AsNoTracking().CountAsync()).Should().Be(2);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("profile")]
    [InlineData("links")]
    public async Task AuthorMerge_rejects_stale_profile_or_book_sets(string mutation)
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using var context = factory.Create(library.DirectoryPath);
        var source = new AuthorEntity { Id = Guid.NewGuid(), Name = "Source", NormalizedName = "source" };
        var target = new AuthorEntity { Id = Guid.NewGuid(), Name = "Target", NormalizedName = "target" };
        context.Authors.AddRange(source, target);
        await context.SaveChangesAsync();
        var repository = new EfAuthorProfileRepository(factory, library.DirectoryPath);
        var preview = (await repository.PreviewMergeAsync([source.Id, target.Id], default))!;
        if (mutation == "name") source.Name = "Changed";
        if (mutation == "profile") target.Biography = "Changed";
        if (mutation == "links") context.Books.Add(new BookEntity { Id = Guid.NewGuid(), Title = "Added", NormalizedTitle = "added", DuplicateKey = "added",
            BookAuthors = [new() { AuthorId = source.Id, Order = 0 }] });
        await context.SaveChangesAsync();
        (await repository.MergeAsync(preview, new(target.Id, null, null, null), null, default)).Status.Should().Be(AuthorMergeStatus.Conflict);
        (await context.Authors.AsNoTracking().CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task AuthorMerge_handles_510_books_and_refuses_invalid_choices_and_cancellation()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using var context = factory.Create(library.DirectoryPath);
        var source = new AuthorEntity { Id = Guid.NewGuid(), Name = "Source", NormalizedName = "source" };
        var target = new AuthorEntity { Id = Guid.NewGuid(), Name = "Target", NormalizedName = "target" };
        context.Authors.AddRange(source, target);
        for (var i = 0; i < 510; i++) context.Books.Add(new() { Id = Guid.NewGuid(), Title = $"Book {i:D3}", NormalizedTitle = $"book {i:D3}",
            DuplicateKey = DuplicateKeyNormalizer.BuildDuplicateKey($"Book {i:D3}", ["Source"]), BookAuthors = [new() { AuthorId = source.Id, Order = 0 }] });
        await context.SaveChangesAsync();
        var repository = new EfAuthorProfileRepository(factory, library.DirectoryPath);
        (await repository.PreviewMergeAsync([source.Id], default)).Should().BeNull();
        var preview = (await repository.PreviewMergeAsync([source.Id, target.Id], default))!;
        preview.BookCount.Should().Be(510);
        (await repository.MergeAsync(preview, new(Guid.NewGuid(), null, null, null), null, default)).Status.Should().Be(AuthorMergeStatus.InvalidSelection);
        (await repository.MergeAsync(preview, new(target.Id, source.Id, null, null), null, default)).Status.Should().Be(AuthorMergeStatus.InvalidSelection);
        (await repository.MergeAsync(preview, new(target.Id, null, null, null), "../photo.jpg", default)).Status.Should().Be(AuthorMergeStatus.InvalidSelection);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await FluentActions.Awaiting(() => repository.MergeAsync(preview, new(target.Id, null, null, null), null, cancelled.Token)).Should().ThrowAsync<OperationCanceledException>();
        var result = await repository.MergeAsync(preview, new(target.Id, null, null, null), null, default);
        result.Status.Should().Be(AuthorMergeStatus.Merged);
        result.Books.Should().HaveCount(510);
        (await context.BookAuthors.AsNoTracking().CountAsync(link => link.AuthorId == target.Id)).Should().Be(510);
    }
}

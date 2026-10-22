using EbookManager.Domain.Authors;
using EbookManager.Infrastructure.Persistence;
using EbookManager.Infrastructure.Persistence.Entities;
using EbookManager.Infrastructure.Persistence.Repositories;
using EbookManager.Tests.TestSupport;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Tests.Infrastructure;

public sealed class AuthorProfileRepositoryTests
{
    [Fact]
    public async Task Saves_only_profile_fields_and_rejects_a_stale_update()
    {
        using var directory = new TemporarySqliteLibrary();
        var factory = new LibraryDbContextFactory();
        await using (var context = factory.Create(directory.DirectoryPath))
        {
            await context.Database.MigrateAsync();
            context.Authors.Add(new AuthorEntity { Id = Guid.NewGuid(), Name = "Author", NormalizedName = "author" });
            await context.SaveChangesAsync();
        }
        var repository = new EfAuthorProfileRepository(factory, directory.DirectoryPath);
        var page = await repository.SearchAsync("author", 0, 200, default);
        page.TotalCount.Should().Be(0); // Empty unused authors do not appear in the overview.
        Guid id;
        await using (var context = factory.Create(directory.DirectoryPath))
        {
            id = await context.Authors.Select(x => x.Id).SingleAsync();
        }
        var original = (await repository.GetAsync(id, default))!;
        var updated = original with { Biography = "Biography", WebsiteUrl = "https://example.org/" };
        (await repository.SaveAsync(original, updated, default)).Should().Be(AuthorProfileSaveStatus.Saved);
        (await repository.SaveAsync(original, original with { Biography = "Stale" }, default))
            .Should().Be(AuthorProfileSaveStatus.Conflict);
        (await repository.GetAsync(id, default)).Should().Be(updated);
        (await repository.SearchAsync("AUTHOR", 0, 1, default)).TotalCount.Should().Be(1);
        (await repository.SearchAsync("author", 1, 1, default)).Authors.Should().BeEmpty();
        SqliteConnection.ClearAllPools();
    }

    [Theory]
    [InlineData("file:///c:/test")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://user:secret@example.org")]
    [InlineData("not a website")]
    public async Task Rejects_unsafe_website_without_changing_profile(string website)
    {
        using var directory = new TemporarySqliteLibrary();
        var factory = new LibraryDbContextFactory();
        var id = Guid.NewGuid();
        await using (var context = factory.Create(directory.DirectoryPath))
        {
            await context.Database.MigrateAsync();
            context.Authors.Add(new AuthorEntity { Id = id, Name = "Author", NormalizedName = "AUTHOR" });
            await context.SaveChangesAsync();
        }
        var repository = new EfAuthorProfileRepository(factory, directory.DirectoryPath);
        var original = (await repository.GetAsync(id, default))!;
        var act = () => repository.SaveAsync(original, original with { WebsiteUrl = website }, default);
        await act.Should().ThrowAsync<ArgumentException>();
        (await repository.GetAsync(id, default)).Should().Be(original);
        SqliteConnection.ClearAllPools();
    }
}

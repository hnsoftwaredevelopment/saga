using EbookManager.Application.Authors;
using EbookManager.Domain.Authors;
using EbookManager.Infrastructure.Files;
using EbookManager.Infrastructure.Persistence;
using EbookManager.Infrastructure.Persistence.Entities;
using EbookManager.Infrastructure.Persistence.Repositories;
using EbookManager.Tests.TestSupport;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Tests.Infrastructure;

public sealed class AuthorProfileServiceTests
{
    [Fact]
    public async Task Replacement_is_committed_before_old_photo_is_removed_and_conflict_keeps_current_photo()
    {
        using var directory = new TemporarySqliteLibrary();
        var factory = new LibraryDbContextFactory();
        var id = Guid.NewGuid();
        await using (var context = factory.Create(directory.DirectoryPath))
        {
            await context.Database.MigrateAsync();
            context.Authors.Add(new AuthorEntity { Id = id, Name = "Author", NormalizedName = "author" });
            await context.SaveChangesAsync();
        }
        var repository = new EfAuthorProfileRepository(factory, directory.DirectoryPath);
        var photos = new ManagedAuthorPhotoStore(directory.DirectoryPath);
        var service = new AuthorProfileService(repository, photos);
        var original = (await repository.GetAsync(id, default))!;
        byte[] jpeg = [0xff, 0xd8, 0xff, 0xd9];
        var first = await service.SaveAsync(original, "bio", null, jpeg, false, default);
        first.Status.Should().Be(AuthorProfileSaveStatus.Saved);
        var second = await service.SaveAsync(first.Profile!, "updated", null, jpeg, false, default);
        (await photos.ReadAsync(id, first.Profile!.PhotoRelativePath!, default)).Should().BeNull();
        (await photos.ReadAsync(id, second.Profile!.PhotoRelativePath!, default)).Should().Equal(jpeg);

        var conflict = await service.SaveAsync(original, "stale", null, jpeg, false, default);
        conflict.Status.Should().Be(AuthorProfileSaveStatus.Conflict);
        (await repository.GetAsync(id, default)).Should().Be(second.Profile);
        Directory.GetFiles(Path.Combine(directory.DirectoryPath, "authors"), "*.jpg", SearchOption.AllDirectories)
            .Should().ContainSingle();
        SqliteConnection.ClearAllPools();
    }
}

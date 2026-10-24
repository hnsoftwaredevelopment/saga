using EbookManager.Domain.Series;
using EbookManager.Infrastructure.Persistence.Entities;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Fact]
    public async Task Series_rename_preserves_metadata_and_changes_all_pages_atomically()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using (var context = factory.Create(library.DirectoryPath))
        {
            for (var index = 0; index < 510; index++) context.Books.Add(new BookEntity
            {
                Id = Guid.NewGuid(), Title = $"Book {index}", NormalizedTitle = $"book {index}", DuplicateKey = $"key-{index}",
                Series = index % 2 == 0 ? " SAS " : "sas", SeriesNumber = index + 0.5m,
                Publisher = "Publisher", CoverBytes = [1, 2, 3]
            });
            await context.SaveChangesAsync();
        }
        var repository = new EfSeriesRepository(factory, library.DirectoryPath);
        var preview = await repository.PreviewRenameAsync("SAS", default);
        preview.Books.Should().HaveCount(510);
        var result = await repository.RenameAsync(preview, " New name ", default);
        result.Status.Should().Be(SeriesRenameStatus.Renamed);
        result.Books.Should().HaveCount(510);
        await using var verify = factory.Create(library.DirectoryPath);
        var books = await verify.Books.AsNoTracking().ToListAsync();
        books.Should().OnlyContain(book => book.Series == "New name" && book.Publisher == "Publisher" && book.SeriesNumber != null);
        books.Should().OnlyContain(book => book.DuplicateKey == "key-" + book.Title.Substring(5));
        books.Should().OnlyContain(book => book.CoverBytes!.SequenceEqual(new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public async Task Series_rename_rejects_collision_and_stale_snapshot_but_allows_case_correction()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var id = Guid.NewGuid();
        await using (var context = factory.Create(library.DirectoryPath))
        {
            context.Books.Add(new BookEntity { Id = id, Title = "One", NormalizedTitle = "one", DuplicateKey = "one", Series = "SAS" });
            context.Books.Add(new BookEntity { Id = Guid.NewGuid(), Title = "Two", NormalizedTitle = "two", DuplicateKey = "two", Series = "Other" });
            await context.SaveChangesAsync();
        }
        var repository = new EfSeriesRepository(factory, library.DirectoryPath);
        var preview = await repository.PreviewRenameAsync("SAS", default);
        (await repository.RenameAsync(preview, " other ", default)).Status.Should().Be(SeriesRenameStatus.NameExists);
        (await repository.RenameAsync(preview, " ", default)).Status.Should().Be(SeriesRenameStatus.InvalidName);
        (await repository.RenameAsync(preview, "SAS", default)).Status.Should().Be(SeriesRenameStatus.Unchanged);
        (await repository.RenameAsync(preview, "sas", default)).Status.Should().Be(SeriesRenameStatus.Renamed);
        (await repository.RenameAsync(preview, "New", default)).Status.Should().Be(SeriesRenameStatus.Conflict);
        var fresh = await repository.PreviewRenameAsync("sas", default);
        await using (var context = factory.Create(library.DirectoryPath))
            await context.Books.Where(book => book.Id == id).ExecuteUpdateAsync(set => set.SetProperty(book => book.Series, " sas "));
        (await repository.RenameAsync(fresh, "New", default)).Status.Should().Be(SeriesRenameStatus.Conflict);
    }

    [Fact]
    public async Task Series_rename_rolls_back_all_updates_on_database_failure()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using (var context = factory.Create(library.DirectoryPath))
        {
            for (var index = 0; index < 510; index++) context.Books.Add(new BookEntity
            { Id = Guid.NewGuid(), Title = $"Book {index}", NormalizedTitle = $"book {index}", DuplicateKey = $"key-{index}", Series = "Old" });
            await context.SaveChangesAsync();
            await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_series BEFORE UPDATE OF Series ON Books WHEN (SELECT COUNT(*) FROM Books WHERE Series = 'New') > 500 BEGIN SELECT RAISE(ABORT, 'test failure'); END;");
        }
        var repository = new EfSeriesRepository(factory, library.DirectoryPath);
        var preview = await repository.PreviewRenameAsync("Old", default);
        await FluentActions.Awaiting(() => repository.RenameAsync(preview, "New", default)).Should().ThrowAsync<Microsoft.Data.Sqlite.SqliteException>();
        await using var verify = factory.Create(library.DirectoryPath);
        (await verify.Books.CountAsync(book => book.Series == "Old")).Should().Be(510);
    }
}

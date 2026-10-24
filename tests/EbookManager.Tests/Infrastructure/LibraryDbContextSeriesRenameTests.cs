using EbookManager.Domain.Series;
using EbookManager.Infrastructure.Persistence.Entities;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Fact]
    public async Task Series_rename_preserves_all_scalar_metadata_and_related_records()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var book = CreateBook("Book", ["Primary", "Coauthor"], ["Tag one", "Tag two"], new DateOnly(2020, 1, 2), coverBytes: [1, 2, 3]);
        await books.AddAsync(book, CreateFile(book.Id), default);
        await using var context = factory.Create(library.DirectoryPath);
        var entity = await context.Books.SingleAsync();
        context.CustomMetadataValues.Add(new CustomMetadataValueEntity
        {
            BookId = entity.Id, TextValue = "Preserve me", Field = new CustomMetadataFieldEntity
            { Id = Guid.NewGuid(), Key = "notes", Name = "Notes", NormalizedName = "notes" }
        });
        await context.SaveChangesAsync();
        var scalars = context.Entry(entity).Properties.Where(property => property.Metadata.Name is not ("Series" or "UpdatedUtc"))
            .ToDictionary(property => property.Metadata.Name, property => property.CurrentValue);
        var authorIds = await context.BookAuthors.OrderBy(link => link.Order).Select(link => new { link.AuthorId, link.Order }).ToArrayAsync();
        var tagIds = await context.BookTags.Select(link => link.TagId).ToArrayAsync();
        var file = await context.BookFiles.AsNoTracking().SingleAsync();
        var repository = new EfSeriesRepository(factory, library.DirectoryPath);
        (await repository.RenameAsync(await repository.PreviewRenameAsync("Series", default), "Renamed", default)).Status.Should().Be(SeriesRenameStatus.Renamed);
        await context.Entry(entity).ReloadAsync();
        context.Entry(entity).Properties.Where(property => property.Metadata.Name is not ("Series" or "UpdatedUtc"))
            .ToDictionary(property => property.Metadata.Name, property => property.CurrentValue).Should().BeEquivalentTo(scalars);
        (await context.BookAuthors.OrderBy(link => link.Order).Select(link => new { link.AuthorId, link.Order }).ToArrayAsync()).Should().BeEquivalentTo(authorIds, options => options.WithStrictOrdering());
        (await context.BookTags.Select(link => link.TagId).ToArrayAsync()).Should().BeEquivalentTo(tagIds);
        (await context.BookFiles.AsNoTracking().SingleAsync()).Should().BeEquivalentTo(file);
        (await context.CustomMetadataValues.AsNoTracking().SingleAsync()).TextValue.Should().Be("Preserve me");
    }

    [Fact]
    public async Task Series_rename_rejects_added_books_and_cancellation_without_changes()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using var context = factory.Create(library.DirectoryPath);
        context.Books.Add(new BookEntity { Id = Guid.NewGuid(), Title = "One", NormalizedTitle = "one", DuplicateKey = "one", Series = "Old" });
        await context.SaveChangesAsync();
        var repository = new EfSeriesRepository(factory, library.DirectoryPath);
        var preview = await repository.PreviewRenameAsync("Old", default);
        context.Books.Add(new BookEntity { Id = Guid.NewGuid(), Title = "Two", NormalizedTitle = "two", DuplicateKey = "two", Series = "Old" });
        await context.SaveChangesAsync();
        (await repository.RenameAsync(preview, "New", default)).Status.Should().Be(SeriesRenameStatus.Conflict);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await FluentActions.Awaiting(() => repository.RenameAsync(preview, "New", cancelled.Token)).Should().ThrowAsync<OperationCanceledException>();
        (await context.Books.CountAsync(book => book.Series == "Old")).Should().Be(2);
    }

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

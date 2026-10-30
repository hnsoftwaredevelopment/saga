using EbookManager.Domain.Series;
using EbookManager.Infrastructure.Persistence.Entities;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Fact]
    public async Task SeriesMerge_validates_targets_and_rolls_back_even_after_first_batch()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using var context = factory.Create(library.DirectoryPath);
        for (var i = 0; i < 511; i++) context.Books.Add(new() { Id = Guid.NewGuid(), Title = $"Book {i}", NormalizedTitle = $"book {i}", DuplicateKey = $"key{i}", Series = i == 510 ? "Series" : "S.e.r.i.e.s", SeriesNumber = i + 0.5m });
        await context.SaveChangesAsync();
        var repository = new EfSeriesRepository(factory, library.DirectoryPath);
        (await repository.PreviewMergeAsync(["Series", " series "], default)).Should().BeNull();
        var preview = (await repository.PreviewMergeAsync(["Series", "S.e.r.i.e.s"], default))!;
        preview.BookCount.Should().Be(511);
        (await repository.MergeAsync(preview, "New", default)).Status.Should().Be(SeriesMergeStatus.InvalidSelection);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await FluentActions.Awaiting(() => repository.MergeAsync(preview, "Series", cancelled.Token)).Should().ThrowAsync<OperationCanceledException>();
        await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_late_merge BEFORE UPDATE OF Series ON Books WHEN (SELECT COUNT(*) FROM Books WHERE Series = 'Series') > 500 BEGIN SELECT RAISE(ABORT, 'late failure'); END;");
        await FluentActions.Awaiting(() => repository.MergeAsync(preview, "Series", default)).Should().ThrowAsync<Microsoft.Data.Sqlite.SqliteException>();
        (await context.Books.CountAsync(book => book.Series == "S.e.r.i.e.s")).Should().Be(510);
        await context.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_late_merge;");
        (await repository.MergeAsync(preview, "Series", default)).Books.Should().HaveCount(510);
        (await context.Books.AsNoTracking().ToArrayAsync()).Should().OnlyContain(book => book.Series == "Series" && book.SeriesNumber != null);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SeriesMerge_changes_only_series_and_timestamps_and_preserves_duplicate_and_missing_numbers(bool failWrite)
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using var context = factory.Create(library.DirectoryPath);
        var names = new[] { "Bob-Evers", "Bob-Evers", "Bob Evers", "Bob Evers", "Other" };
        decimal?[] numbers = [1m, 1.5m, 1m, null, 9m];
        for (var i = 0; i < names.Length; i++) context.Books.Add(new() { Id = Guid.NewGuid(), Title = $"Book {i}", NormalizedTitle = $"book {i}", DuplicateKey = $"key-{i}", Series = names[i], SeriesNumber = numbers[i], UpdatedUtc = DateTimeOffset.UnixEpoch, CoverBytes = [1, 2, 3] });
        await context.SaveChangesAsync();
        var originals = await context.Books.AsNoTracking().ToArrayAsync();
        var repository = new EfSeriesRepository(factory, library.DirectoryPath);
        var preview = (await repository.PreviewMergeAsync(["Bob-Evers", "Bob Evers"], default))!;
        preview.BookCount.Should().Be(4);
        if (failWrite)
        {
            await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_series_merge BEFORE UPDATE OF Series ON Books BEGIN SELECT RAISE(ABORT, 'test failure'); END;");
            await FluentActions.Awaiting(() => repository.MergeAsync(preview, "Bob Evers", default)).Should().ThrowAsync<Microsoft.Data.Sqlite.SqliteException>();
            (await context.Books.AsNoTracking().ToArrayAsync()).Should().BeEquivalentTo(originals);
            await context.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_series_merge;");
        }
        var result = await repository.MergeAsync(preview, "Bob Evers", default);
        result.Status.Should().Be(SeriesMergeStatus.Merged);
        result.Books.Should().HaveCount(2);
        var saved = await context.Books.AsNoTracking().ToArrayAsync();
        foreach (var original in originals)
        {
            var updated = saved.Single(book => book.Id == original.Id);
            updated.Should().BeEquivalentTo(original, options => options.Excluding(book => book.Series).Excluding(book => book.UpdatedUtc));
            if (original.Series == "Bob-Evers") { updated.Series.Should().Be("Bob Evers"); updated.UpdatedUtc.Should().BeAfter(original.UpdatedUtc); }
            else { updated.Series.Should().Be(original.Series); updated.UpdatedUtc.Should().Be(original.UpdatedUtc); }
        }
        (await repository.SearchAsync("Bob", 0, 200, default)).Series.Single().BookCount.Should().Be(4);
    }

    [Theory]
    [InlineData("number")]
    [InlineData("name")]
    [InlineData("title")]
    [InlineData("added")]
    public async Task SeriesMerge_rejects_changed_confirmation_data_without_overwriting_new_values(string mutation)
    {
        using var library = new TemporaryLibrary(); var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using var context = factory.Create(library.DirectoryPath);
        var first = new BookEntity { Id = Guid.NewGuid(), Title = "First", NormalizedTitle = "first", DuplicateKey = "first", Series = "Bob-Evers", SeriesNumber = 1 };
        var second = new BookEntity { Id = Guid.NewGuid(), Title = "Second", NormalizedTitle = "second", DuplicateKey = "second", Series = "Bob Evers", SeriesNumber = 2 };
        context.Books.AddRange(first, second); await context.SaveChangesAsync();
        var repository = new EfSeriesRepository(factory, library.DirectoryPath);
        var preview = (await repository.PreviewMergeAsync(["Bob-Evers", "Bob Evers"], default))!;
        if (mutation == "number") first.SeriesNumber = 99;
        if (mutation == "name") first.Series = "Unrelated";
        if (mutation == "title") first.Title = "New title";
        if (mutation == "added") context.Books.Add(new() { Id = Guid.NewGuid(), Title = "Added", NormalizedTitle = "added", DuplicateKey = "added", Series = "Bob Evers" });
        await context.SaveChangesAsync();
        var expected = await context.Books.AsNoTracking().ToArrayAsync();
        (await repository.MergeAsync(preview, "Bob Evers", default)).Status.Should().Be(SeriesMergeStatus.Conflict);
        (await context.Books.AsNoTracking().ToArrayAsync()).Should().BeEquivalentTo(expected);
    }
}

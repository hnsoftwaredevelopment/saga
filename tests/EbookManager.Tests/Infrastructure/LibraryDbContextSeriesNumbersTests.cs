using EbookManager.Domain.Series;
using EbookManager.Infrastructure.Persistence.Entities;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Fact]
    public async Task SeriesNumbers_rejects_incomplete_selection_and_rolls_back_all_writes_on_failure_or_cancellation()
    {
        using var library = new TemporaryLibrary(); var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using var context = factory.Create(library.DirectoryPath);
        var rows = Enumerable.Range(0, 2).Select(i => new BookEntity { Id = Guid.NewGuid(), Title = $"Book {i}", NormalizedTitle = $"book {i}", DuplicateKey = $"book {i}", Series = "Series", SeriesNumber = 9 }).ToArray();
        context.Books.AddRange(rows); await context.SaveChangesAsync();
        var repo = new EfSeriesRepository(factory, library.DirectoryPath); var ids = rows.Select(row => row.Id).ToArray();
        (await repo.PreviewNumbersAsync("Other", ids, default)).Should().BeNull();
        (await repo.PreviewNumbersAsync("Series", [ids[0], ids[0]], default)).Should().BeNull();
        var preview = (await repo.PreviewNumbersAsync("Series", ids, default))!;
        (await repo.ChangeNumbersAsync(preview, [new(ids[0], 1)], default)).Status.Should().Be(SeriesNumberStatus.Conflict);
        (await repo.ChangeNumbersAsync(preview, [new(ids[0], 1), new(ids[0], 2)], default)).Status.Should().Be(SeriesNumberStatus.Conflict);
        var assignments = new SeriesNumberAssignment[] { new(ids[0], 1), new(ids[1], 2) };
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await FluentActions.Awaiting(() => repo.ChangeNumbersAsync(preview, assignments, cancellation.Token)).Should().ThrowAsync<OperationCanceledException>();
        await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_numbers BEFORE UPDATE OF SeriesNumber ON Books WHEN NEW.SeriesNumber = '2.0' OR NEW.SeriesNumber = '2' BEGIN SELECT RAISE(ABORT, 'test failure'); END;");
        await FluentActions.Awaiting(() => repo.ChangeNumbersAsync(preview, assignments, default)).Should().ThrowAsync<DbUpdateException>();
        (await context.Books.AsNoTracking().ToArrayAsync()).Should().BeEquivalentTo(rows);
    }

    [Fact]
    public async Task SeriesNumbers_updates_only_selected_numbers_across_batches_preserves_new_metadata_and_reports_duplicates()
    {
        using var library = new TemporaryLibrary(); var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using var context = factory.Create(library.DirectoryPath);
        var rows = Enumerable.Range(0, 503).Select(i => new BookEntity { Id = Guid.NewGuid(), Title = $"Book {i}", NormalizedTitle = $"book {i}", DuplicateKey = $"book {i}", Series = i % 2 == 0 ? "Series" : " SERIES ", SeriesNumber = null, CoverBytes = [1,2,3] }).ToArray();
        var untouched = new BookEntity { Id = Guid.NewGuid(), Title = "Unselected", NormalizedTitle = "unselected", DuplicateKey = "unselected", Series = "Series", SeriesNumber = 1 };
        context.Books.AddRange(rows.Append(untouched)); await context.SaveChangesAsync();
        var repo = new EfSeriesRepository(factory, library.DirectoryPath);
        var ids = rows.Reverse().Select(book => book.Id).ToArray();
        var preview = await repo.PreviewNumbersAsync("series", ids, default);
        preview.Should().NotBeNull(); preview!.Books.Select(row => row.Book.Id).Should().Equal(ids);
        await context.Books.Where(book => book.Id == ids[0]).ExecuteUpdateAsync(set => set.SetProperty(book => book.Publisher, "New publisher"));
        var before = await context.Books.AsNoTracking().ToArrayAsync();
        var result = await repo.ChangeNumbersAsync(preview, ids.Select((id,i) => new SeriesNumberAssignment(id, i + 1)).ToArray(), default);
        result.Status.Should().Be(SeriesNumberStatus.Changed); result.Books.Should().HaveCount(503); result.MatchingOtherBooks.Should().Be(1);
        var after = await context.Books.AsNoTracking().ToArrayAsync();
        foreach (var previous in before)
        {
            var saved = after.Single(book => book.Id == previous.Id);
            if (previous.Id == untouched.Id) saved.Should().BeEquivalentTo(previous);
            else
            {
                saved.Should().BeEquivalentTo(previous, options => options.Excluding(book => book.SeriesNumber).Excluding(book => book.UpdatedUtc));
                saved.SeriesNumber.Should().Be(Array.IndexOf(ids, saved.Id) + 1);
            }
        }
    }

    [Theory]
    [InlineData("number")][InlineData("name")][InlineData("title")][InlineData("author")][InlineData("removed")]
    public async Task SeriesNumbers_stale_selected_book_blocks_the_entire_transaction(string mutation)
    {
        using var library = new TemporaryLibrary(); var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using var context = factory.Create(library.DirectoryPath);
        var first = new BookEntity { Id = Guid.NewGuid(), Title = "First", NormalizedTitle = "first", DuplicateKey = "first", Series = "Series", SeriesNumber = 9 };
        var second = new BookEntity { Id = Guid.NewGuid(), Title = "Second", NormalizedTitle = "second", DuplicateKey = "second", Series = "Series", SeriesNumber = 8 };
        context.Books.AddRange(first, second); await context.SaveChangesAsync();
        var repo = new EfSeriesRepository(factory, library.DirectoryPath);
        var preview = await repo.PreviewNumbersAsync("Series", [first.Id, second.Id], default); preview.Should().NotBeNull();
        if (mutation == "number") second.SeriesNumber = 99;
        if (mutation == "name") second.Series = "Other";
        if (mutation == "title") second.Title = "New title";
        if (mutation == "author") context.BookAuthors.Add(new() { BookId = second.Id, Order = 0, Author = new() { Id = Guid.NewGuid(), Name = "Author", NormalizedName = "author" } });
        if (mutation == "removed") context.Books.Remove(second);
        await context.SaveChangesAsync(); var before = await context.Books.AsNoTracking().ToArrayAsync();
        (await repo.ChangeNumbersAsync(preview!, [new(first.Id, 1), new(second.Id, 2)], default)).Status.Should().Be(SeriesNumberStatus.Conflict);
        (await context.Books.AsNoTracking().ToArrayAsync()).Should().BeEquivalentTo(before);
    }
}

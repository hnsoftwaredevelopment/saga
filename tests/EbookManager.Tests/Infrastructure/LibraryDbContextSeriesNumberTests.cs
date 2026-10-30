using EbookManager.Domain.Series;
using EbookManager.Infrastructure.Persistence.Entities;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Theory]
    [InlineData("1.5")]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task SeriesNumber_changes_one_number_preserving_other_fields_and_new_publisher(string? input)
    {
        using var library = new TemporaryLibrary(); var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var book = CreateBook("First", ["Author", "Coauthor"], ["Tag"], coverBytes: [1, 2, 3]);
        await books.AddAsync(book, CreateFile(book.Id), default);
        await using var context = factory.Create(library.DirectoryPath);
        var other = new BookEntity { Id = Guid.NewGuid(), Title = "Other", NormalizedTitle = "other", DuplicateKey = "other", Series = " SERIES ", SeriesNumber = 1.5m, UpdatedUtc = DateTimeOffset.UnixEpoch };
        context.Books.Add(other); await context.SaveChangesAsync();
        await context.Books.Where(row => row.Id == book.Id).ExecuteUpdateAsync(set => set.SetProperty(row => row.SeriesNumber, 1m));
        var repository = new EfSeriesRepository(factory, library.DirectoryPath);
        var preview = (await repository.PreviewNumberAsync(book.Id, default))!;
        preview.Book.Id.Should().Be(book.Id); preview.OtherNumbers.Should().Equal(1.5m);
        // A publisher change since opening must survive a number-only edit.
        await context.Books.Where(row => row.Id == book.Id).ExecuteUpdateAsync(set => set.SetProperty(row => row.Publisher, "Latest publisher"));
        var before = await context.Books.AsNoTracking().SingleAsync(row => row.Id == book.Id);
        var beforeOther = await context.Books.AsNoTracking().SingleAsync(row => row.Id == other.Id);
        decimal? number = input is null ? null : decimal.Parse(input, CultureInfo.InvariantCulture);
        var result = await repository.ChangeNumberAsync(preview, number, default);
        result.Status.Should().Be(SeriesNumberStatus.Changed);
        result.MatchingOtherBooks.Should().Be(number == 1.5m ? 1 : 0);
        var after = await context.Books.AsNoTracking().SingleAsync(row => row.Id == book.Id);
        after.Should().BeEquivalentTo(before, options => options.Excluding(row => row.SeriesNumber).Excluding(row => row.UpdatedUtc));
        after.SeriesNumber.Should().Be(number);
        (await context.Books.AsNoTracking().SingleAsync(row => row.Id == other.Id)).Should().BeEquivalentTo(beforeOther);
        var saved = (await books.GetAsync(book.Id, default))!;
        saved.Metadata.Authors.Should().Equal(book.Metadata.Authors);
        saved.Metadata.Tags.Should().Equal(book.Metadata.Tags!);
    }

    [Theory]
    [InlineData("number")]
    [InlineData("name")]
    [InlineData("title")]
    [InlineData("author")]
    [InlineData("removed")]
    public async Task SeriesNumber_rejects_stale_book_confirmation(string mutation)
    {
        using var library = new TemporaryLibrary(); var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using var context = factory.Create(library.DirectoryPath);
        var book = new BookEntity { Id = Guid.NewGuid(), Title = "First", NormalizedTitle = "first", DuplicateKey = "first", Series = "Series", SeriesNumber = 1 };
        context.Books.Add(book); await context.SaveChangesAsync();
        var repository = new EfSeriesRepository(factory, library.DirectoryPath);
        var preview = (await repository.PreviewNumberAsync(book.Id, default))!;
        if (mutation == "number") book.SeriesNumber = 9;
        if (mutation == "name") book.Series = "Other";
        if (mutation == "title") book.Title = "New title";
        if (mutation == "author") context.BookAuthors.Add(new() { BookId = book.Id, Order = 0, Author = new() { Id = Guid.NewGuid(), Name = "New author", NormalizedName = "new author" } });
        if (mutation == "removed") context.Books.Remove(book);
        await context.SaveChangesAsync();
        var expected = await context.Books.AsNoTracking().ToArrayAsync();
        (await repository.ChangeNumberAsync(preview, 3, default)).Status.Should().Be(SeriesNumberStatus.Conflict);
        (await context.Books.AsNoTracking().ToArrayAsync()).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SeriesNumber_unchanged_cancellation_and_write_failure_do_not_modify_book()
    {
        using var library = new TemporaryLibrary(); var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using var context = factory.Create(library.DirectoryPath);
        var book = new BookEntity { Id = Guid.NewGuid(), Title = "First", NormalizedTitle = "first", DuplicateKey = "first", Series = "Series", SeriesNumber = 1 };
        context.Books.Add(book); await context.SaveChangesAsync();
        var repository = new EfSeriesRepository(factory, library.DirectoryPath);
        var preview = (await repository.PreviewNumberAsync(book.Id, default))!;
        (await repository.ChangeNumberAsync(preview, 1, default)).Status.Should().Be(SeriesNumberStatus.Unchanged);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await FluentActions.Awaiting(() => repository.ChangeNumberAsync(preview, 3, cancelled.Token)).Should().ThrowAsync<OperationCanceledException>();
        await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_number BEFORE UPDATE OF SeriesNumber ON Books BEGIN SELECT RAISE(ABORT, 'test failure'); END;");
        await FluentActions.Awaiting(() => repository.ChangeNumberAsync(preview, 3, default)).Should().ThrowAsync<Microsoft.Data.Sqlite.SqliteException>();
        (await context.Books.AsNoTracking().SingleAsync()).Should().BeEquivalentTo(book);
    }
}

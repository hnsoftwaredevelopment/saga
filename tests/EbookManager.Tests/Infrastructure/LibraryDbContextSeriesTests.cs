using EbookManager.Infrastructure.Persistence.Entities;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Fact]
    public async Task Series_reads_group_unicode_names_and_sort_decimal_numbers_before_paging()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using (var context = factory.Create(library.DirectoryPath))
        {
            var author = new AuthorEntity { Id = Guid.NewGuid(), Name = "Author", NormalizedName = "author" };
            var numbers = new decimal?[] { 10, 2, 1.5m, 1 }.Concat(Enumerable.Range(0, 205).Select(index => (decimal?)(100 + index))).Append(null).ToArray();
            for (var index = 0; index < numbers.Length; index++)
                context.Books.Add(new BookEntity
                {
                    Id = Guid.NewGuid(), Title = $"Book {index:D3}", NormalizedTitle = $"book {index:D3}", DuplicateKey = $"series-{index}",
                    Series = index % 2 == 0 ? " ÉTÉ " : "été", SeriesNumber = numbers[index], CoverBytes = [1, 2, 3],
                    BookAuthors = [new() { Author = author, Order = 0 }]
                });
            context.Books.Add(new BookEntity { Id = Guid.NewGuid(), Title = "Other", NormalizedTitle = "other", DuplicateKey = "other", Series = "Ete" });
            context.Books.Add(new BookEntity { Id = Guid.NewGuid(), Title = "Empty", NormalizedTitle = "empty", DuplicateKey = "empty", Series = "\u00a0" });
            await context.SaveChangesAsync();
        }
        var repository = new EfSeriesRepository(factory, library.DirectoryPath);

        var names = await repository.SearchAsync(" ÉT ", 0, 200, default);
        names.TotalCount.Should().Be(1);
        names.Series.Single().BookCount.Should().Be(210);
        var first = await repository.ListBooksAsync(" éTÉ ", 0, 200, default);
        var last = await repository.ListBooksAsync("été", 200, 200, default);
        first.Books.Take(4).Select(book => book.Number).Should().Equal(1m, 1.5m, 2m, 10m);
        first.Books.Should().OnlyContain(book => book.Authors == "Author");
        first.TotalCount.Should().Be(210);
        var all = first.Books.Concat(last.Books).ToArray();
        all.Should().HaveCount(210).And.OnlyHaveUniqueItems(book => book.Id);
        all.Last().Number.Should().BeNull();
        (await repository.SearchAsync("", 0, 200, default)).TotalCount.Should().Be(2);
    }
}

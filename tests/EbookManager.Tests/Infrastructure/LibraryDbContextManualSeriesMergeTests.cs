using EbookManager.Domain.Series;
using EbookManager.Infrastructure.Persistence.Entities;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Fact]
    public async Task ManualSeriesMerge_combines_three_unrelated_series_under_a_new_name_preserving_numbers_and_other_books()
    {
        using var library = new TemporaryLibrary(); var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using var context = factory.Create(library.DirectoryPath);
        var names = new[] { "Eerste", "Tweede", "Derde" };
        var rows = Enumerable.Range(0, 503).Select(i => new BookEntity { Id = Guid.NewGuid(), Title = $"Book {i}", NormalizedTitle = $"book {i}", DuplicateKey = $"book {i}", Series = names[i % 3], SeriesNumber = i % 5 == 0 ? null : i % 3 + 0.5m, Publisher = "Publisher", CoverBytes = [1,2,3] }).ToArray();
        var other = new BookEntity { Id = Guid.NewGuid(), Title = "Other", NormalizedTitle = "other", DuplicateKey = "other", Series = "Unselected", SeriesNumber = 99 };
        context.Books.AddRange(rows.Append(other)); await context.SaveChangesAsync();
        var repo = new EfSeriesRepository(factory, library.DirectoryPath);
        var preview = (await repo.PreviewMergeAsync(names, default))! with { AllowNewName = true };
        await context.Books.Where(row => row.Id == rows[0].Id).ExecuteUpdateAsync(set => set.SetProperty(row => row.Publisher, "Latest publisher"));
        var before = await context.Books.AsNoTracking().ToArrayAsync();
        var result = await repo.MergeAsync(preview, "  Gezamenlijke reeks  ", default);
        result.Status.Should().Be(SeriesMergeStatus.Merged); result.Name.Should().Be("Gezamenlijke reeks"); result.Books.Should().HaveCount(503);
        var saved = await context.Books.AsNoTracking().ToArrayAsync();
        foreach (var original in before)
        {
            var updated = saved.Single(row => row.Id == original.Id);
            if (original.Id == other.Id) updated.Should().BeEquivalentTo(original);
            else
            {
                updated.Series.Should().Be("Gezamenlijke reeks");
                updated.Should().BeEquivalentTo(original, options => options.Excluding(row => row.Series).Excluding(row => row.UpdatedUtc));
            }
        }
        (await repo.SearchAsync("", 0, 200, default)).Series.Should().HaveCount(2);
    }
    [Fact]
    public async Task ManualSeriesMerge_refuses_an_unselected_target_even_when_it_appears_after_the_preview()
    {
        using var library = new TemporaryLibrary(); var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using var context = factory.Create(library.DirectoryPath);
        var first = new BookEntity { Id = Guid.NewGuid(), Title = "First", NormalizedTitle = "first", DuplicateKey = "first", Series = "One", SeriesNumber = 1 };
        var second = new BookEntity { Id = Guid.NewGuid(), Title = "Second", NormalizedTitle = "second", DuplicateKey = "second", Series = "Two", SeriesNumber = 1 };
        context.Books.AddRange(first, second); await context.SaveChangesAsync();
        var repo = new EfSeriesRepository(factory, library.DirectoryPath);
        var preview = (await repo.PreviewMergeAsync(["One", "Two"], default))! with { AllowNewName = true };
        context.Books.Add(new() { Id = Guid.NewGuid(), Title = "Other", NormalizedTitle = "other", DuplicateKey = "other", Series = " Existing " }); await context.SaveChangesAsync();
        var before = await context.Books.AsNoTracking().ToArrayAsync();
        (await repo.MergeAsync(preview, "existing", default)).Status.Should().Be(SeriesMergeStatus.NameExists);
        (await context.Books.AsNoTracking().ToArrayAsync()).Should().BeEquivalentTo(before);
        (await repo.MergeAsync(preview, "   ", default)).Status.Should().Be(SeriesMergeStatus.InvalidSelection);
    }
}

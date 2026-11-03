using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Fact]
    public async Task Export_readmodel_keeps_selected_ids_authors_and_files_across_sql_batches_without_writes()
    {
        using var library = new TemporaryLibrary(); var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var repository = new EfBookRepository(factory, library.DirectoryPath);
        var first = CreateBook("Selected", ["First author", "Second author"], coverBytes: [1, 2, 3]);
        await repository.AddAsync(first, CreateFile(first.Id), default);
        await using var context = factory.Create(library.DirectoryPath);
        for (var i = 0; i < 502; i++) context.Books.Add(new() { Id = Guid.NewGuid(), Title = $"Other {i}", NormalizedTitle = $"other {i}", DuplicateKey = $"other {i}", CoverBytes = [1, 2, 3] });
        await context.SaveChangesAsync();
        var before = await context.Books.AsNoTracking().ToArrayAsync();
        var ids = before.Take(501).Select(book => book.Id).Append(first.Id).Append(first.Id).Append(Guid.NewGuid()).ToArray();
        var rows = await new EfBookExportRepository(factory, library.DirectoryPath).ReadAsync(ids, default);
        rows.Select(row => row.Id).Should().BeEquivalentTo(before.Where(book => ids.Contains(book.Id)).Select(book => book.Id));
        var selected = rows.Single(row => row.Id == first.Id);
        selected.Authors.Should().Equal("First author", "Second author"); selected.Files.Should().HaveCount(1);
        (await context.Books.AsNoTracking().ToArrayAsync()).Should().BeEquivalentTo(before);
    }
}

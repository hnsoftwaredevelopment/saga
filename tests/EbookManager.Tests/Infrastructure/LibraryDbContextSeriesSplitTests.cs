using EbookManager.Application.Books;
using EbookManager.Application.Series;
using EbookManager.Domain.Series;
using EbookManager.Infrastructure.Files;
using EbookManager.Infrastructure.Metadata;
using EbookManager.Infrastructure.Persistence.Entities;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Fact]
    public async Task Series_move_into_an_existing_series_preserves_numbers_and_rechecks_overlap()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using var context = factory.Create(library.DirectoryPath);
        var moved = NewSplitBook("Moved", "Original", 2.5m);
        var other = NewSplitBook("Other", " Target ", 1m);
        context.Books.AddRange(moved, other);
        await context.SaveChangesAsync();
        var repo = new EfSeriesRepository(factory, library.DirectoryPath);
        var source = await repo.PreviewRenameAsync("Original", default);
        var preview = source with { SelectedBookIds = [moved.Id], Destination = new("Target", []) };
        await context.Books.Where(book => book.Id == other.Id).ExecuteUpdateAsync(set => set.SetProperty(book => book.SeriesNumber, 2.5m));

        (await repo.RenameAsync(preview, "Target", default)).Status.Should().Be(SeriesRenameStatus.Conflict);
        (await context.Books.AsNoTracking().SingleAsync(book => book.Id == moved.Id)).Series.Should().Be("Original");
        var confirmed = preview with { Destination = new("Target", [2.5m]) };
        var result = await repo.RenameAsync(confirmed, "Target", default);

        result.Status.Should().Be(SeriesRenameStatus.Renamed);
        result.Books.Select(book => book.Id).Should().Equal(moved.Id);
        var saved = await context.Books.AsNoTracking().ToDictionaryAsync(book => book.Id);
        saved[moved.Id].Series.Should().Be("Target");
        saved[moved.Id].SeriesNumber.Should().Be(2.5m);
        saved[other.Id].Series.Should().Be(" Target ");
        saved[other.Id].SeriesNumber.Should().Be(2.5m);
        saved[other.Id].UpdatedUtc.Should().Be(other.UpdatedUtc);
    }

    [Fact]
    public async Task Series_split_moves_only_selected_books_without_changing_their_numbers()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using var context = factory.Create(library.DirectoryPath);
        var first = NewSplitBook("First", "Original", 2.5m);
        var second = NewSplitBook("Second", " original ", null);
        var remaining = NewSplitBook("Remaining", "Original", 10m);
        context.Books.AddRange(first, second, remaining);
        await context.SaveChangesAsync();
        var repo = new EfSeriesRepository(factory, library.DirectoryPath);
        var source = await repo.PreviewRenameAsync("Original", default);
        var preview = source with { SelectedBookIds = [first.Id, second.Id] };

        var result = await repo.RenameAsync(preview, "  New Series  ", default);

        result.Status.Should().Be(SeriesRenameStatus.Renamed);
        result.Books.Select(book => book.Id).Should().BeEquivalentTo(new[] { first.Id, second.Id });
        var saved = await context.Books.AsNoTracking().ToDictionaryAsync(book => book.Id);
        saved[first.Id].Series.Should().Be("New Series");
        saved[first.Id].SeriesNumber.Should().Be(2.5m);
        saved[second.Id].Series.Should().Be("New Series");
        saved[second.Id].SeriesNumber.Should().BeNull();
        saved[remaining.Id].Series.Should().Be("Original");
        saved[remaining.Id].SeriesNumber.Should().Be(10m);
        saved[remaining.Id].UpdatedUtc.Should().Be(remaining.UpdatedUtc);
    }

    [Fact]
    public async Task Series_split_rejects_empty_full_unknown_or_stale_selection_and_name_collision()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        await using var context = factory.Create(library.DirectoryPath);
        var first = NewSplitBook("First", "Original", 1m);
        var second = NewSplitBook("Second", "Original", 2m);
        var other = NewSplitBook("Other", "Other", 1m);
        context.Books.AddRange(first, second, other);
        await context.SaveChangesAsync();
        var repo = new EfSeriesRepository(factory, library.DirectoryPath);
        var source = await repo.PreviewRenameAsync("Original", default);
        Guid[][] invalidSelections = [[], [first.Id, second.Id], [other.Id], [first.Id, first.Id]];
        foreach (var ids in invalidSelections)
            (await repo.RenameAsync(source with { SelectedBookIds = ids }, "New", default)).Status.Should().Be(SeriesRenameStatus.Conflict);

        var preview = source with { SelectedBookIds = [first.Id] };
        (await repo.RenameAsync(preview, " original ", default)).Status.Should().Be(SeriesRenameStatus.NameExists);
        (await repo.RenameAsync(preview, " other ", default)).Status.Should().Be(SeriesRenameStatus.NameExists);
        await context.Books.Where(book => book.Id == first.Id).ExecuteUpdateAsync(set => set.SetProperty(book => book.SeriesNumber, 3m));
        (await repo.RenameAsync(preview, "New", default)).Status.Should().Be(SeriesRenameStatus.Conflict);
        (await context.Books.CountAsync(book => book.Series == "Original")).Should().Be(2);
    }

    [Fact]
    public async Task Series_split_updates_only_the_moved_books_sidecar()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var first = CreateBook("First", ["Author"]);
        var second = CreateBook("Second", ["Author"]);
        await books.AddAsync(first, CreateFile(first.Id, Hash(1)), default);
        await books.AddAsync(second, CreateFile(second.Id, Hash(2)), default);
        var repository = new EfSeriesRepository(factory, library.DirectoryPath);
        var sidecars = new JsonMetadataSidecarStore();
        var service = new SeriesRenameService(repository, new BookService(books, new ManagedLibraryFileStore(library.DirectoryPath),
            new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));
        var preview = await service.PreviewAsync(first.Metadata.Series!, default);

        var result = await service.RenameAsync(preview with { SelectedBookIds = [first.Id] }, "New series", default);

        result.Change.Status.Should().Be(SeriesRenameStatus.Renamed);
        result.Warnings.Should().BeEmpty();
        var path = Directory.GetFiles(library.DirectoryPath, "metadata.json", SearchOption.AllDirectories).Should().ContainSingle().Subject;
        var stored = await sidecars.TryReadAsync(path, default);
        stored!.Title.Should().Be(first.Metadata.Title);
        stored.Series.Should().Be("New series");
        stored.SeriesNumber.Should().Be(first.Metadata.SeriesNumber);
        (await books.GetAsync(second.Id, default))!.Metadata.Series.Should().Be(second.Metadata.Series);
    }

    private static BookEntity NewSplitBook(string title, string series, decimal? number) => new()
    {
        Id = Guid.NewGuid(), Title = title, NormalizedTitle = title.ToLowerInvariant(), DuplicateKey = title,
        Series = series, SeriesNumber = number
    };
}

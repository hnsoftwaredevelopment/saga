using EbookManager.Application.Books;
using EbookManager.Application.Series;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Series;
using EbookManager.Infrastructure.Files;
using EbookManager.Infrastructure.Metadata;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task SeriesNumbers_sync_finishes_all_changed_books_after_commit_despite_cancellation(bool fail)
    {
        using var library = new TemporaryLibrary(); var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var first = CreateBook("First", ["Author"], ["Tag"]); var second = CreateBook("Second", ["Author"], ["Tag"]);
        await books.AddAsync(first, CreateFile(first.Id, Hash(1)), default); await books.AddAsync(second, CreateFile(second.Id, Hash(2)), default);
        using var cancellation = new CancellationTokenSource();
        var repository = new CancelAfterNumbersCommit(new EfSeriesRepository(factory, library.DirectoryPath), cancellation);
        var sidecars = new TagSidecars(fail);
        var service = new SeriesNumbersService(repository, new BookService(books, new ManagedLibraryFileStore(library.DirectoryPath), new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));
        var preview = (await service.PreviewAsync(first.Metadata.Series!, [first.Id, second.Id], cancellation.Token))!;
        var result = await service.ChangeAsync(preview, [new(first.Id, 101), new(second.Id, 102)], cancellation.Token);
        result.Change.Status.Should().Be(SeriesNumberStatus.Changed); result.Warnings.Count.Should().Be(fail ? 2 : 0);
        sidecars.Writes.Should().Be(2); cancellation.IsCancellationRequested.Should().BeTrue();
        (await books.GetAsync(first.Id, default))!.Metadata.SeriesNumber.Should().Be(101);
        (await books.GetAsync(second.Id, default))!.Metadata.SeriesNumber.Should().Be(102);
    }
    [Fact]
    public async Task SeriesNumbers_writes_real_sidecars_only_for_changed_numbers()
    {
        using var library = new TemporaryLibrary(); var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var first = CreateBook("First", ["Author"]); var second = CreateBook("Second", ["Author"]);
        await books.AddAsync(first, CreateFile(first.Id, Hash(1)), default); await books.AddAsync(second, CreateFile(second.Id, Hash(2)), default);
        var repository = new EfSeriesRepository(factory, library.DirectoryPath); var sidecars = new JsonMetadataSidecarStore();
        var service = new SeriesNumbersService(repository, new BookService(books, new ManagedLibraryFileStore(library.DirectoryPath), new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));
        var assignments = new SeriesNumberAssignment[] { new(first.Id, 101), new(second.Id, 102) };
        var changed = await service.ChangeAsync((await service.PreviewAsync(first.Metadata.Series!, [first.Id, second.Id], default))!, assignments, default);
        changed.Warnings.Should().BeEmpty();
        var files = Directory.GetFiles(library.DirectoryPath, "metadata.json", SearchOption.AllDirectories);
        files.Should().HaveCount(2);
        var stored = await Task.WhenAll(files.Select(file => sidecars.TryReadAsync(file, default)));
        stored.Select(row => row!.SeriesNumber).Should().BeEquivalentTo(new decimal?[] { 101, 102 });
        foreach (var file in files) File.Delete(file);
        (await service.ChangeAsync((await service.PreviewAsync(first.Metadata.Series!, [first.Id, second.Id], default))!, assignments, default)).Change.Status.Should().Be(SeriesNumberStatus.Unchanged);
        Directory.GetFiles(library.DirectoryPath, "metadata.json", SearchOption.AllDirectories).Should().BeEmpty();
    }
    private sealed class CancelAfterNumbersCommit(ISeriesNumbersRepository inner, CancellationTokenSource cancellation) : ISeriesNumbersRepository
    {
        public Task<SeriesNumbersPreview?> PreviewNumbersAsync(string name, IReadOnlyList<Guid> ids, CancellationToken token) => inner.PreviewNumbersAsync(name, ids, token);
        public async Task<SeriesNumbersResult> ChangeNumbersAsync(SeriesNumbersPreview preview, IReadOnlyList<SeriesNumberAssignment> assignments, CancellationToken token)
        { var result = await inner.ChangeNumbersAsync(preview, assignments, token); cancellation.Cancel(); return result; }
    }
}

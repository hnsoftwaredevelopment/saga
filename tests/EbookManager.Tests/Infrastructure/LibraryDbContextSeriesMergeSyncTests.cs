using EbookManager.Application.Books;
using EbookManager.Application.Series;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Series;
using EbookManager.Infrastructure.Files;
using EbookManager.Infrastructure.Metadata;
using EbookManager.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Tests.Infrastructure;

public sealed partial class LibraryDbContextTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SeriesMerge_sync_finishes_after_commit_and_reports_file_warnings(bool fail)
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var first = CreateBook("First", ["Author"], ["Tag"]);
        var second = CreateBook("Second", ["Other"], ["Tag"]);
        await books.AddAsync(first, CreateFile(first.Id, Hash(1)), default);
        await books.AddAsync(second, CreateFile(second.Id, Hash(2)), default);
        await using (var context = factory.Create(library.DirectoryPath))
            await context.Books.Where(book => book.Id == second.Id).ExecuteUpdateAsync(set => set.SetProperty(book => book.Series, "S.e.r.i.e.s"));
        using var cancellation = new CancellationTokenSource();
        var repository = new CancelAfterSeriesMergeCommit(new EfSeriesRepository(factory, library.DirectoryPath), cancellation);
        var sidecars = new TagSidecars(fail);
        var service = new SeriesMergeService(repository, new BookService(books, new ManagedLibraryFileStore(library.DirectoryPath),
            new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));
        var preview = (await service.PreviewAsync(["Series", "S.e.r.i.e.s"], cancellation.Token))!;
        var result = await service.MergeAsync(preview, "Series", cancellation.Token);
        cancellation.IsCancellationRequested.Should().BeTrue();
        result.Change.Status.Should().Be(SeriesMergeStatus.Merged);
        result.Change.Books.Select(book => book.Id).Should().Equal(second.Id);
        result.Warnings.Count.Should().Be(fail ? 1 : 0);
        sidecars.Writes.Should().Be(1);
        var saved = (await books.GetAsync(second.Id, default))!.Metadata;
        saved.Series.Should().Be("Series");
        saved.SeriesNumber.Should().Be(second.Metadata.SeriesNumber);
        saved.Authors.Should().Equal("Other");
        saved.Tags.Should().Equal("Tag");
    }

    [Fact]
    public async Task SeriesMerge_sync_writes_real_metadata_json_with_original_number()
    {
        using var library = new TemporaryLibrary();
        var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var first = CreateBook("First", ["Author"]);
        var second = CreateBook("Second", ["Other"]);
        await books.AddAsync(first, CreateFile(first.Id, Hash(1)), default);
        await books.AddAsync(second, CreateFile(second.Id, Hash(2)), default);
        await using (var context = factory.Create(library.DirectoryPath))
            await context.Books.Where(book => book.Id == second.Id).ExecuteUpdateAsync(set => set.SetProperty(book => book.Series, "S.e.r.i.e.s"));
        var repository = new EfSeriesRepository(factory, library.DirectoryPath);
        var sidecars = new JsonMetadataSidecarStore();
        var service = new SeriesMergeService(repository, new BookService(books, new ManagedLibraryFileStore(library.DirectoryPath),
            new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));
        var result = await service.MergeAsync((await service.PreviewAsync(["Series", "S.e.r.i.e.s"], default))!, "Series", default);
        result.Warnings.Should().BeEmpty();
        var metadata = (await sidecars.TryReadAsync(Directory.GetFiles(library.DirectoryPath, "metadata.json", SearchOption.AllDirectories).Single(), default))!;
        metadata.Series.Should().Be("Series");
        metadata.SeriesNumber.Should().Be(second.Metadata.SeriesNumber);
    }

    private sealed class CancelAfterSeriesMergeCommit(ISeriesMergeRepository inner, CancellationTokenSource cancellation) : ISeriesMergeRepository
    {
        public Task<SeriesMergePreview?> PreviewMergeAsync(IReadOnlyCollection<string> names, CancellationToken token) => inner.PreviewMergeAsync(names, token);
        public async Task<SeriesMergeResult> MergeAsync(SeriesMergePreview preview, string target, CancellationToken token)
        {
            var result = await inner.MergeAsync(preview, target, token);
            cancellation.Cancel();
            return result;
        }
    }
}

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
    [InlineData(false)]
    [InlineData(true)]
    public async Task SeriesNumber_sync_finishes_after_commit_and_reports_file_warning(bool fail)
    {
        using var library = new TemporaryLibrary(); var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var book = CreateBook("Book", ["Author"], ["Tag"]);
        await books.AddAsync(book, CreateFile(book.Id), default);
        using var cancellation = new CancellationTokenSource();
        var repository = new CancelAfterNumberCommit(new EfSeriesRepository(factory, library.DirectoryPath), cancellation);
        var sidecars = new TagSidecars(fail);
        var service = new SeriesNumberService(repository, new BookService(books, new ManagedLibraryFileStore(library.DirectoryPath),
            new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));
        var preview = (await service.PreviewAsync(book.Id, cancellation.Token))!;
        var result = await service.ChangeAsync(preview, 3.5m, cancellation.Token);
        result.Change.Status.Should().Be(SeriesNumberStatus.Changed);
        result.FileWarning.Should().Be(fail);
        cancellation.IsCancellationRequested.Should().BeTrue();
        sidecars.Writes.Should().Be(1);
        var saved = (await books.GetAsync(book.Id, default))!;
        saved.Metadata.SeriesNumber.Should().Be(3.5m);
        saved.Metadata.Series.Should().Be(book.Metadata.Series);
        saved.Metadata.Authors.Should().Equal(book.Metadata.Authors);
        saved.Metadata.Tags.Should().Equal(book.Metadata.Tags!);
    }

    [Fact]
    public async Task SeriesNumber_sync_writes_real_metadata_json_and_unchanged_does_not_write_again()
    {
        using var library = new TemporaryLibrary(); var factory = await CreateMigratedFactoryAsync(library.DirectoryPath);
        var books = new EfBookRepository(factory, library.DirectoryPath);
        var book = CreateBook("Book", ["Author"]);
        await books.AddAsync(book, CreateFile(book.Id), default);
        var repository = new EfSeriesRepository(factory, library.DirectoryPath);
        var sidecars = new JsonMetadataSidecarStore();
        var service = new SeriesNumberService(repository, new BookService(books, new ManagedLibraryFileStore(library.DirectoryPath),
            new MetadataAdapterResolver([new FallbackMetadataAdapter()]), sidecars));
        (await service.ChangeAsync((await service.PreviewAsync(book.Id, default))!, null, default)).FileWarning.Should().BeFalse();
        var file = Directory.GetFiles(library.DirectoryPath, "metadata.json", SearchOption.AllDirectories).Single();
        (await sidecars.TryReadAsync(file, default))!.SeriesNumber.Should().BeNull();
        File.Delete(file);
        (await service.ChangeAsync((await service.PreviewAsync(book.Id, default))!, null, default)).Change.Status.Should().Be(SeriesNumberStatus.Unchanged);
        File.Exists(file).Should().BeFalse();
    }

    private sealed class CancelAfterNumberCommit(ISeriesNumberRepository inner, CancellationTokenSource cancellation) : ISeriesNumberRepository
    {
        public Task<SeriesNumberPreview?> PreviewNumberAsync(Guid id, CancellationToken token) => inner.PreviewNumberAsync(id, token);
        public async Task<SeriesNumberResult> ChangeNumberAsync(SeriesNumberPreview preview, decimal? number, CancellationToken token)
        { var result = await inner.ChangeNumberAsync(preview, number, token); cancellation.Cancel(); return result; }
    }
}

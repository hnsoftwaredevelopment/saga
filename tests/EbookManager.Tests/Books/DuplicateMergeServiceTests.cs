using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using FluentAssertions;

namespace EbookManager.Tests.Books;

public sealed class DuplicateMergeServiceTests
{
    [Fact]
    public async Task Merge_formats_only_attaches_source_files_to_target_without_metadata_overwrite()
    {
        var sourceBookId = Guid.NewGuid();
        var targetBookId = Guid.NewGuid();
        var repository = new RecordingBookRepository();
        var service = new DuplicateMergeService(repository);

        var result = await service.MergeFormatsOnlyAsync(
            sourceBookId,
            targetBookId,
            default);

        result.SourceBookId.Should().Be(sourceBookId);
        result.TargetBookId.Should().Be(targetBookId);
        result.MetadataPolicy.Should().Be(DuplicateMergeMetadataPolicy.PreserveTarget);
        repository.AttachedSourceBookId.Should().Be(sourceBookId);
        repository.AttachedTargetBookId.Should().Be(targetBookId);
    }

    private sealed class RecordingBookRepository : IBookRepository
    {
        public Guid? AttachedSourceBookId { get; private set; }
        public Guid? AttachedTargetBookId { get; private set; }

        public Task<IReadOnlyList<Book>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Book>>([]);

        public Task<Book?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Book?>(null);

        public Task<bool> HasHashAsync(string sha256, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> HasNormalizedTitleAndAuthorAsync(
            string title,
            IReadOnlyList<string> authors,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<Book?> FindByNormalizedTitleAndAuthorAsync(
            string title,
            IReadOnlyList<string> authors,
            CancellationToken cancellationToken) =>
            Task.FromResult<Book?>(null);

        public Task<IReadOnlyList<Book>> FindByNormalizedTitleAsync(
            string title,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Book>>([]);

        public Task AddAsync(Book book, BookFile file, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AddFileAsync(BookFile file, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AttachFilesToBookAsync(
            Guid sourceBookId,
            Guid targetBookId,
            CancellationToken cancellationToken)
        {
            AttachedSourceBookId = sourceBookId;
            AttachedTargetBookId = targetBookId;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Book book, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<BookFile>> ListFilesAsync(Guid bookId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BookFile>>([]);

        public Task UpdateFileWriteBackAsync(
            Guid fileId,
            MetadataWriteResult result,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}

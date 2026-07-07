using EbookManager.Domain.Abstractions;

namespace EbookManager.Application.Books;

public sealed class DuplicateMergeService(IBookRepository bookRepository)
{
    public async Task<DuplicateMergeResult> MergeFormatsOnlyAsync(
        Guid sourceBookId,
        Guid targetBookId,
        CancellationToken cancellationToken)
    {
        await bookRepository.AttachFilesToBookAsync(sourceBookId, targetBookId, cancellationToken);
        return new DuplicateMergeResult(
            sourceBookId,
            targetBookId,
            DuplicateMergeMetadataPolicy.PreserveTarget);
    }
}

public enum DuplicateMergeMetadataPolicy
{
    PreserveTarget
}

public sealed record DuplicateMergeResult(
    Guid SourceBookId,
    Guid TargetBookId,
    DuplicateMergeMetadataPolicy MetadataPolicy);

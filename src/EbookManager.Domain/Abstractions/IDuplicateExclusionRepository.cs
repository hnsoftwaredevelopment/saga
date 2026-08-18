using EbookManager.Domain.Books;

namespace EbookManager.Domain.Abstractions;

public interface IDuplicateExclusionRepository
{
    Task<IReadOnlySet<DuplicateExclusionPair>> ListDuplicateExclusionsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<DuplicateExclusion>> ListDuplicateExclusionDetailsAsync(CancellationToken cancellationToken);

    Task AddDuplicateExclusionsAsync(
        IReadOnlyCollection<DuplicateExclusionPair> pairs,
        CancellationToken cancellationToken);

    Task RemoveDuplicateExclusionsAsync(
        IReadOnlyCollection<DuplicateExclusionPair> pairs,
        CancellationToken cancellationToken);

    Task ClearDuplicateExclusionsAsync(CancellationToken cancellationToken);
}

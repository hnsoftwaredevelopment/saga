using EbookManager.Domain.Books;

namespace EbookManager.Domain.Abstractions;

public interface IDuplicateExclusionRepository
{
    Task<IReadOnlySet<DuplicateExclusionPair>> ListDuplicateExclusionsAsync(CancellationToken cancellationToken);

    Task AddDuplicateExclusionsAsync(
        IReadOnlyCollection<DuplicateExclusionPair> pairs,
        CancellationToken cancellationToken);
}

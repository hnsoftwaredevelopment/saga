namespace EbookManager.Domain.Abstractions;

public interface IBookCoverStore
{
    Task<string> SaveAsync(
        Guid bookId,
        byte[] coverBytes,
        CancellationToken cancellationToken);

    Task DeleteAsync(Guid bookId, CancellationToken cancellationToken);
}

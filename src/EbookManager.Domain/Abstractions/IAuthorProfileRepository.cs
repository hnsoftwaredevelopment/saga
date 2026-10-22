using EbookManager.Domain.Authors;

namespace EbookManager.Domain.Abstractions;

public interface IAuthorProfileRepository
{
    Task<AuthorPage> SearchAsync(string query, int skip, int take, CancellationToken cancellationToken);
    Task<AuthorProfile?> GetAsync(Guid authorId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuthorBook>> ListBooksAsync(Guid authorId, int skip, int take, CancellationToken cancellationToken);
    Task<AuthorProfileSaveStatus> SaveAsync(AuthorProfile original, AuthorProfile updated, CancellationToken cancellationToken);
}

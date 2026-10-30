using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Authors;

namespace EbookManager.Application.Authors;

public interface IAuthorDuplicateIndex
{
    Task<IReadOnlyList<AuthorSummary>> LoadAsync(CancellationToken token);
}

public sealed class AuthorDuplicateSearchService(IAuthorProfileRepository repository) : IAuthorDuplicateIndex
{
    public async Task<IReadOnlyList<AuthorSummary>> LoadAsync(CancellationToken token)
    {
        var authors = new List<AuthorSummary>();
        var skip = 0;
        AuthorPage page;
        do
        {
            token.ThrowIfCancellationRequested();
            page = await repository.SearchAsync("", skip, 500, token);
            authors.AddRange(page.Authors);
            skip += page.Authors.Count;
        } while (page.Authors.Count > 0 && skip < page.TotalCount);
        return authors.DistinctBy(author => author.Id).ToArray();
    }
}

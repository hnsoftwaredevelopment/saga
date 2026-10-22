using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Authors;
using EbookManager.Domain.Books;

namespace EbookManager.Application.Authors;

public sealed record AuthorRenameCompletion(AuthorRenameResult Change, IReadOnlyList<AuthorBook> Warnings);

public interface IAuthorRenameService
{
    Task<AuthorRenamePreview?> PreviewAsync(Guid authorId, CancellationToken cancellationToken);
    Task<AuthorRenameCompletion> RenameAsync(AuthorRenamePreview preview, string name, CancellationToken cancellationToken);
}

public sealed class AuthorRenameService(IAuthorRenameRepository repository, BookService books) : IAuthorRenameService
{
    public Task<AuthorRenamePreview?> PreviewAsync(Guid authorId, CancellationToken cancellationToken) =>
        repository.PreviewRenameAsync(authorId, cancellationToken);

    public async Task<AuthorRenameCompletion> RenameAsync(AuthorRenamePreview preview, string name, CancellationToken cancellationToken)
    {
        var change = await repository.RenameAsync(preview, name, cancellationToken);
        if (change.Status != AuthorRenameStatus.Renamed) return new(change, []);
        var warnings = new List<AuthorBook>();
        // The library is already committed. A cancellation or file error must not hide that success.
        foreach (var book in change.Books)
        {
            try
            {
                var result = await books.SynchronizeStoredMetadataAsync(book.Id, CancellationToken.None);
                if (result.Status != BookSaveStatus.Succeeded ||
                    result.FileResults.Any(file => file.Result.Status == MetadataWriteBackStatus.Failed))
                    warnings.Add(book);
            }
            catch (Exception) { warnings.Add(book); }
        }
        return new(change, warnings.AsReadOnly());
    }
}

using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Authors;
using EbookManager.Domain.Books;

namespace EbookManager.Application.Authors;

public sealed record AuthorMergeCompletion(AuthorMergeResult Change, IReadOnlyList<AuthorBook> Warnings, bool CleanupWarning = false);
public interface IAuthorMergeService
{
    Task<AuthorMergePreview?> PreviewAsync(IReadOnlyCollection<Guid> authorIds, CancellationToken token);
    Task<AuthorMergeCompletion> MergeAsync(AuthorMergePreview preview, AuthorMergeSelection selection, CancellationToken token);
}

public sealed class AuthorMergeService(IAuthorMergeRepository repository, IAuthorProfileRepository profiles,
    IAuthorPhotoStore photos, BookService books) : IAuthorMergeService
{
    public Task<AuthorMergePreview?> PreviewAsync(IReadOnlyCollection<Guid> authorIds, CancellationToken token) => repository.PreviewMergeAsync(authorIds, token);

    public async Task<AuthorMergeCompletion> MergeAsync(AuthorMergePreview preview, AuthorMergeSelection selection, CancellationToken token)
    {
        var target = preview.Authors.FirstOrDefault(author => author.Profile.Id == selection.TargetId)?.Profile;
        var photoSource = preview.Authors.FirstOrDefault(author => author.Profile.Id == selection.PhotoAuthorId)?.Profile;
        if (target is null || selection.PhotoAuthorId is not null && photoSource?.PhotoRelativePath is null)
            return new(new(AuthorMergeStatus.InvalidSelection, null, []), []);
        string? newPath = null;
        var photoPath = selection.PhotoAuthorId == target.Id ? target.PhotoRelativePath : null;
        if (photoSource is not null && photoSource.Id != target.Id)
        {
            var bytes = await photos.ReadAsync(photoSource.Id, photoSource.PhotoRelativePath!, token)
                ?? throw new IOException("Selected author photo is unavailable.");
            newPath = photoPath = await photos.SaveJpegAsync(target.Id, bytes, token);
        }
        AuthorMergeResult change;
        try { change = await repository.MergeAsync(preview, selection, photoPath, token); }
        catch
        {
            // A failed response must never delete a photo still referenced by the saved target.
            if (newPath is not null) await TryCleanupAsync(target.Id, newPath);
            throw;
        }
        if (change.Status != AuthorMergeStatus.Merged)
        {
            var failedCleanup = newPath is not null && !await TryCleanupAsync(target.Id, newPath);
            return new(change, [], failedCleanup);
        }
        var cleanupWarning = false;
        foreach (var author in preview.Authors.Select(author => author.Profile))
            if (author.PhotoRelativePath is { } oldPath && (author.Id != target.Id || oldPath != photoPath))
                cleanupWarning |= !await TryCleanupAsync(author.Id, oldPath);
        var warnings = new List<AuthorBook>();
        // The database is committed: finish metadata synchronization even after request cancellation.
        foreach (var book in change.Books)
        {
            try
            {
                var result = await books.SynchronizeStoredMetadataAsync(book.Id, CancellationToken.None);
                if (result.Status != BookSaveStatus.Succeeded || result.FileResults.Any(file => file.Result.Status == MetadataWriteBackStatus.Failed))
                    warnings.Add(book);
            }
            catch (Exception) { warnings.Add(book); }
        }
        return new(change, warnings, cleanupWarning);
    }

    private async Task<bool> TryCleanupAsync(Guid id, string path)
    {
        try
        {
            if ((await profiles.GetAsync(id, CancellationToken.None))?.PhotoRelativePath == path) return true;
            await photos.DeleteAsync(id, path, CancellationToken.None);
            return true;
        }
        catch (Exception) { return false; }
    }
}

using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Authors;

namespace EbookManager.Application.Authors;

public sealed record AuthorProfileSaveResult(AuthorProfileSaveStatus Status, AuthorProfile? Profile, bool CleanupWarning = false);

public sealed class AuthorProfileService(IAuthorProfileRepository repository, IAuthorPhotoStore photos)
{
    public async Task<AuthorProfileSaveResult> SaveAsync(
        AuthorProfile original, string? biography, string? website, byte[]? newJpeg, bool removePhoto,
        CancellationToken cancellationToken)
    {
        AuthorProfileValidation.Validate(biography, website);
        var newPath = newJpeg is null ? null : await photos.SaveJpegAsync(original.Id, newJpeg, cancellationToken);
        var updated = original with
        {
            Biography = AuthorProfileValidation.NormalizeOptional(biography),
            WebsiteUrl = AuthorProfileValidation.NormalizeOptional(website),
            PhotoRelativePath = newPath ?? (removePhoto ? null : original.PhotoRelativePath)
        };
        AuthorProfileSaveStatus status;
        try
        {
            status = await repository.SaveAsync(original, updated, cancellationToken);
        }
        catch
        {
            // Cancellation or an I/O failure can occur after commit. Never delete a referenced photo.
            var current = await repository.GetAsync(original.Id, CancellationToken.None);
            if (current == updated)
            {
                status = AuthorProfileSaveStatus.Saved;
            }
            else
            {
                if (newPath is not null && current?.PhotoRelativePath != newPath)
                    await TryDeleteAsync(original.Id, newPath);
                throw;
            }
        }

        var cleanupWarning = false;
        if (status == AuthorProfileSaveStatus.Conflict)
        {
            if (newPath is not null) cleanupWarning = !await TryDeleteAsync(original.Id, newPath);
            return new(status, null, cleanupWarning);
        }
        if (original.PhotoRelativePath is not null && original.PhotoRelativePath != updated.PhotoRelativePath)
            cleanupWarning = !await TryDeleteAsync(original.Id, original.PhotoRelativePath);
        return new(status, updated, cleanupWarning);
    }

    private async Task<bool> TryDeleteAsync(Guid authorId, string path)
    {
        try
        {
            await photos.DeleteAsync(authorId, path, CancellationToken.None);
            return true;
        }
        catch (Exception) { return false; }
    }
}

using EbookManager.Domain.Authors;
using EbookManager.Domain.Books;
using EbookManager.Application.Authors;
using EbookManager.Application.Metadata;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Infrastructure.Persistence.Repositories;

public sealed partial class EfAuthorProfileRepository
{
    public async Task<AuthorMergePreview?> PreviewMergeAsync(IReadOnlyCollection<Guid> authorIds, CancellationToken token)
    {
        var ids = authorIds.Distinct().Order().ToArray();
        if (ids.Length < 2 || ids.Contains(Guid.Empty)) return null;
        await using var context = factory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        return await MergeSnapshotAsync(context, ids, token);
    }

    public async Task<AuthorMergeResult> MergeAsync(AuthorMergePreview preview, AuthorMergeSelection selection, string? targetPhotoPath, CancellationToken token)
    {
        AuthorMergeResult Reject(AuthorMergeStatus status) => new(status, null, []);
        var ids = preview.Authors.Select(author => author.Profile.Id).Order().ToArray();
        if (ids.Length < 2 || ids.Distinct().Count() != ids.Length || ids.Contains(Guid.Empty) || !ids.Contains(selection.TargetId))
            return Reject(AuthorMergeStatus.InvalidSelection);
        var profiles = preview.Authors.ToDictionary(author => author.Profile.Id, author => author.Profile);
        bool HasValue(Guid? id, Func<AuthorProfile, string?> field) => id is null || profiles.TryGetValue(id.Value, out var profile) && !string.IsNullOrWhiteSpace(field(profile));
        if (!HasValue(selection.BiographyAuthorId, profile => profile.Biography) ||
            !HasValue(selection.WebsiteAuthorId, profile => profile.WebsiteUrl) || !HasValue(selection.PhotoAuthorId, profile => profile.PhotoRelativePath))
            return Reject(AuthorMergeStatus.InvalidSelection);
        string? Value(Guid? id, Func<AuthorProfile, string?> field) => id is null ? null : field(profiles[id.Value]);
        var biography = Value(selection.BiographyAuthorId, profile => profile.Biography);
        var website = Value(selection.WebsiteAuthorId, profile => profile.WebsiteUrl);
        if (biography?.Length > 10000 || !AuthorProfileValidation.IsValidWebsite(website) ||
            !ValidMergePhoto(selection, profiles, targetPhotoPath)) return Reject(AuthorMergeStatus.InvalidSelection);

        await using var context = factory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        var current = await MergeSnapshotAsync(context, ids, token);
        if (current is null || current.Authors.Any(author => !preview.Authors.Any(old => old.Profile == author.Profile &&
            old.BookIds.Order().SequenceEqual(author.BookIds)))) return Reject(AuthorMergeStatus.Conflict);
        var sourceIds = ids.Where(id => id != selection.TargetId).ToArray();
        // Project only metadata needed for final author ordering and duplicate keys, never cover blobs.
        var rows = await context.BookAuthors.AsNoTracking().Where(link =>
                link.Book.BookAuthors.Any(source => sourceIds.Contains(source.AuthorId)))
            .OrderBy(link => link.BookId).ThenBy(link => link.Order)
            .Select(link => new { link.BookId, link.AuthorId, link.Order, link.Author.Name, link.Book.Title, link.Book.Series }).ToArrayAsync(token);
        var target = await context.Authors.SingleAsync(author => author.Id == selection.TargetId, token);
        var grouped = rows.GroupBy(row => row.BookId).ToArray();
        var keys = new Dictionary<Guid, string>();
        var replacements = new List<(Guid BookId, int Order)>();
        foreach (var group in grouped)
        {
            var selectedLinks = group.Where(row => ids.Contains(row.AuthorId)).ToArray();
            var kept = selectedLinks.FirstOrDefault(row => row.AuthorId == target.Id) ?? selectedLinks[0];
            var names = group.Where(row => !ids.Contains(row.AuthorId) || row == kept)
                .Select(row => row == kept ? target.Name : row.Name).ToArray();
            keys[group.Key] = DuplicateKeyNormalizer.BuildDuplicateKey(group.First().Title, names);
            if (kept.AuthorId != target.Id) replacements.Add((group.Key, kept.Order));
        }
        if (keys.Values.Distinct(StringComparer.Ordinal).Count() != keys.Count) return Reject(AuthorMergeStatus.DuplicateBook);
        var affectedIds = keys.Keys.ToHashSet();
        foreach (var batch in keys.Values.Chunk(400))
        {
            var collisions = await context.Books.AsNoTracking().Where(book => batch.Contains(book.DuplicateKey)).Select(book => book.Id).ToArrayAsync(token);
            if (collisions.Any(id => !affectedIds.Contains(id))) return Reject(AuthorMergeStatus.DuplicateBook);
        }
        try
        {
            await context.BookAuthors.Where(link => sourceIds.Contains(link.AuthorId)).ExecuteDeleteAsync(token);
            foreach (var replacement in replacements) context.BookAuthors.Add(new()
                { BookId = replacement.BookId, AuthorId = target.Id, Order = replacement.Order });
            await context.SaveChangesAsync(token);
            var updated = DateTimeOffset.UtcNow;
            foreach (var (id, key) in keys)
            {
                if (await context.Books.Where(book => book.Id == id).ExecuteUpdateAsync(setters => setters
                    .SetProperty(book => book.DuplicateKey, key).SetProperty(book => book.UpdatedUtc, updated), token) != 1)
                    return Reject(AuthorMergeStatus.Conflict);
            }
            target.Biography = biography;
            target.WebsiteUrl = website;
            target.PhotoRelativePath = targetPhotoPath;
            await context.Authors.Where(author => sourceIds.Contains(author.Id)).ExecuteDeleteAsync(token);
            await context.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteErrorCode: 19 })
        { return Reject(AuthorMergeStatus.Conflict); }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        { return Reject(AuthorMergeStatus.Conflict); }
        return new(AuthorMergeStatus.Merged, new(target.Id, target.Name, target.Biography, target.WebsiteUrl, target.PhotoRelativePath),
            grouped.Select(group => new AuthorBook(group.Key, group.First().Title, group.First().Series)).ToArray());
    }

    private static bool ValidMergePhoto(AuthorMergeSelection selection, Dictionary<Guid, AuthorProfile> profiles, string? path)
    {
        if (selection.PhotoAuthorId is null) return path is null;
        if (selection.PhotoAuthorId == selection.TargetId) return path == profiles[selection.TargetId].PhotoRelativePath;
        var id = selection.TargetId.ToString("N");
        var prefix = $"authors/{id[..2]}/{id}-";
        return path is not null && path.StartsWith(prefix, StringComparison.Ordinal) && path.EndsWith(".jpg", StringComparison.Ordinal) &&
            Guid.TryParseExact(path[prefix.Length..^4], "N", out _);
    }

    private static async Task<AuthorMergePreview?> MergeSnapshotAsync(LibraryDbContext context, Guid[] ids, CancellationToken token)
    {
        var profiles = await context.Authors.AsNoTracking().Where(author => ids.Contains(author.Id))
            .Select(author => new AuthorProfile(author.Id, author.Name, author.Biography, author.WebsiteUrl, author.PhotoRelativePath)).ToArrayAsync(token);
        if (profiles.Length != ids.Length || profiles.Any(profile => !MetadataQualityAuthorRules.IsUsable(profile.Name))) return null;
        var links = await context.BookAuthors.AsNoTracking().Where(link => ids.Contains(link.AuthorId)).OrderBy(link => link.BookId)
            .Select(link => new { link.AuthorId, link.BookId }).ToArrayAsync(token);
        return new(profiles.OrderBy(profile => profile.Id).Select(profile => new AuthorRenamePreview(profile,
            links.Where(link => link.AuthorId == profile.Id).Select(link => link.BookId).ToArray())).ToArray());
    }
}

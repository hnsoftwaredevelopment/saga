using EbookManager.Application.Metadata;
using EbookManager.Domain.Authors;
using EbookManager.Domain.Books;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Infrastructure.Persistence.Repositories;

public sealed partial class EfAuthorProfileRepository
{
    public async Task<AuthorRenamePreview?> PreviewRenameAsync(Guid authorId, CancellationToken cancellationToken)
    {
        await using var context = factory.Create(libraryPath);
        var profile = await context.Authors.AsNoTracking().Where(x => x.Id == authorId)
            .Select(x => new AuthorProfile(x.Id, x.Name, x.Biography, x.WebsiteUrl, x.PhotoRelativePath))
            .SingleOrDefaultAsync(cancellationToken);
        if (profile is null) return null;
        var ids = await context.BookAuthors.Where(x => x.AuthorId == authorId).OrderBy(x => x.BookId)
            .Select(x => x.BookId).ToListAsync(cancellationToken);
        return new(profile, ids.AsReadOnly());
    }

    public async Task<AuthorRenameResult> RenameAsync(AuthorRenamePreview preview, string newName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(preview);
        if (!MetadataQualityAuthorRules.IsUsable(newName)) return Rejected(AuthorRenameStatus.InvalidName);
        newName = newName.Trim();
        var normalized = newName.ToLowerInvariant();
        await using var context = factory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var author = await context.Authors.SingleOrDefaultAsync(x => x.Id == preview.Profile.Id, cancellationToken);
        if (author is null || author.Name != preview.Profile.Name) return Rejected(AuthorRenameStatus.Conflict);
        var ids = await context.BookAuthors.Where(x => x.AuthorId == author.Id).OrderBy(x => x.BookId)
            .Select(x => x.BookId).ToListAsync(cancellationToken);
        if (!ids.SequenceEqual(preview.BookIds.Order())) return Rejected(AuthorRenameStatus.Conflict);
        if (author.Name == newName) return Rejected(AuthorRenameStatus.Unchanged);
        if (await context.Authors.AnyAsync(x => x.Id != author.Id && x.NormalizedName == normalized, cancellationToken))
            return Rejected(AuthorRenameStatus.NameExists);

        // Project only identifiers, titles and ordered author names; never load cover blobs for a rename.
        var rows = await context.BookAuthors.AsNoTracking()
            .Where(x => x.Book.BookAuthors.Any(target => target.AuthorId == author.Id))
            .OrderBy(x => x.BookId).ThenBy(x => x.Order)
            .Select(x => new { x.BookId, x.Book.Title, x.Book.Series, x.AuthorId, x.Author.Name })
            .ToListAsync(cancellationToken);
        var keys = rows.GroupBy(x => x.BookId).ToDictionary(group => group.Key,
            group => DuplicateKeyNormalizer.BuildDuplicateKey(group.First().Title,
                group.Select(x => x.AuthorId == author.Id ? newName : x.Name).ToArray()));
        if (keys.Values.Distinct(StringComparer.Ordinal).Count() != keys.Count)
            return Rejected(AuthorRenameStatus.DuplicateBook);
        var affectedIds = ids.ToHashSet();
        foreach (var batch in keys.Values.Chunk(400))
        {
            var collisions = await context.Books.AsNoTracking().Where(x => batch.Contains(x.DuplicateKey))
                .Select(x => x.Id).ToListAsync(cancellationToken);
            if (collisions.Any(id => !affectedIds.Contains(id))) return Rejected(AuthorRenameStatus.DuplicateBook);
        }

        try
        {
            var updatedUtc = DateTimeOffset.UtcNow;
            foreach (var (bookId, key) in keys)
            {
                await context.Books.Where(x => x.Id == bookId).ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.DuplicateKey, key).SetProperty(x => x.UpdatedUtc, updatedUtc), cancellationToken);
            }
            author.Name = newName;
            author.NormalizedName = normalized;
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            return Rejected(AuthorRenameStatus.Conflict);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            return Rejected(AuthorRenameStatus.DuplicateBook);
        }
        return new(AuthorRenameStatus.Renamed,
            new(author.Id, author.Name, author.Biography, author.WebsiteUrl, author.PhotoRelativePath),
            rows.GroupBy(row => row.BookId).Select(group =>
                new AuthorBook(group.Key, group.First().Title, group.First().Series)).ToArray());
    }

    private static AuthorRenameResult Rejected(AuthorRenameStatus status) => new(status, null, []);
}

using EbookManager.Application.Authors;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Authors;
using EbookManager.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Infrastructure.Persistence.Repositories;

public sealed class EfAuthorProfileRepository(LibraryDbContextFactory factory, string libraryPath) : IAuthorProfileRepository
{
    public async Task<AuthorPage> SearchAsync(string query, int skip, int take, CancellationToken cancellationToken)
    {
        ValidatePage(skip, take);
        await using var context = factory.Create(libraryPath);
        var authors = context.Authors.AsNoTracking().Where(x =>
            x.NormalizedName != "unknown" && x.Name.Trim() != "" &&
            (x.BookAuthors.Any() || x.Biography != null || x.WebsiteUrl != null || x.PhotoRelativePath != null));
        var normalized = query.Trim().ToLowerInvariant();
        if (normalized.Length > 0)
        {
            authors = authors.Where(x => x.NormalizedName.Contains(normalized));
        }
        var count = await authors.CountAsync(cancellationToken);
        var page = await authors.OrderBy(x => x.NormalizedName).ThenBy(x => x.Id)
            .Skip(skip).Take(take)
            .Select(x => new AuthorSummary(x.Id, x.Name, x.BookAuthors.Count))
            .ToListAsync(cancellationToken);
        return new AuthorPage(page, count);
    }

    public async Task<AuthorProfile?> GetAsync(Guid authorId, CancellationToken cancellationToken)
    {
        await using var context = factory.Create(libraryPath);
        return await context.Authors.AsNoTracking().Where(x => x.Id == authorId)
            .Select(x => new AuthorProfile(x.Id, x.Name, x.Biography, x.WebsiteUrl, x.PhotoRelativePath))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuthorBook>> ListBooksAsync(Guid authorId, int skip, int take, CancellationToken cancellationToken)
    {
        ValidatePage(skip, take);
        await using var context = factory.Create(libraryPath);
        return await context.BookAuthors.AsNoTracking().Where(x => x.AuthorId == authorId)
            .OrderBy(x => x.Book.NormalizedTitle).ThenBy(x => x.BookId).Skip(skip).Take(take)
            .Select(x => new AuthorBook(x.BookId, x.Book.Title, x.Book.Series)).ToListAsync(cancellationToken);
    }

    public async Task<AuthorProfileSaveStatus> SaveAsync(AuthorProfile original, AuthorProfile updated, CancellationToken cancellationToken)
    {
        if (original.Id == Guid.Empty || original.Id != updated.Id || original.Name != updated.Name)
        {
            throw new ArgumentException("Author identity cannot be changed by profile editing.");
        }
        AuthorProfileValidation.Validate(updated.Biography, updated.WebsiteUrl);
        await using var context = factory.Create(libraryPath);
        var affected = await context.Authors.Where(x => x.Id == original.Id && x.Name == original.Name &&
                x.Biography == original.Biography && x.WebsiteUrl == original.WebsiteUrl && x.PhotoRelativePath == original.PhotoRelativePath)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Biography, AuthorProfileValidation.NormalizeOptional(updated.Biography))
                .SetProperty(x => x.WebsiteUrl, AuthorProfileValidation.NormalizeOptional(updated.WebsiteUrl))
                .SetProperty(x => x.PhotoRelativePath, updated.PhotoRelativePath), cancellationToken);
        return affected == 1 ? AuthorProfileSaveStatus.Saved : AuthorProfileSaveStatus.Conflict;
    }

    private static void ValidatePage(int skip, int take)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        if (take is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(take));
    }
}

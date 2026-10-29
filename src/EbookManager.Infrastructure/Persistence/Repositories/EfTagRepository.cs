using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Tags;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Infrastructure.Persistence.Repositories;

public sealed partial class EfTagRepository(LibraryDbContextFactory factory, string libraryPath) : ITagRepository, ITagAssignmentRepository
{
    public async Task<TagPage> SearchAsync(string query, int skip, int take, CancellationToken token)
    {
        ValidatePage(skip, take);
        await using var context = factory.Create(libraryPath);
        var normalized = TagNames.Normalize(query);
        var tags = context.Tags.AsNoTracking().Where(tag => tag.BookTags.Any() && tag.NormalizedName.Contains(normalized));
        var count = await tags.CountAsync(token);
        var rows = await tags.OrderBy(tag => tag.NormalizedName).ThenBy(tag => tag.Id).Skip(skip).Take(take)
            .Select(tag => new TagSummary(tag.Id, tag.Name, tag.BookTags.Count)).ToArrayAsync(token);
        return new(rows, count);
    }

    public async Task<TagBookPage> ListBooksAsync(Guid tagId, int skip, int take, CancellationToken token)
    {
        ValidatePage(skip, take);
        await using var context = factory.Create(libraryPath);
        var query = context.Books.AsNoTracking().Where(book => book.BookTags.Any(link => link.TagId == tagId));
        var count = await query.CountAsync(token);
        var page = await query.OrderBy(book => book.NormalizedTitle).ThenBy(book => book.Id).Skip(skip).Take(take)
            .Select(book => new { book.Id, book.Title }).ToArrayAsync(token);
        var ids = page.Select(book => book.Id).ToArray();
        var authors = await context.BookAuthors.AsNoTracking().Where(link => ids.Contains(link.BookId))
            .OrderBy(link => link.Order).Select(link => new { link.BookId, link.Author.Name }).ToListAsync(token);
        var byBook = authors.GroupBy(link => link.BookId).ToDictionary(group => group.Key, group => string.Join(", ", group.Select(link => link.Name)));
        return new(page.Select(book => new TagBook(book.Id, book.Title, byBook.GetValueOrDefault(book.Id, ""))).ToArray(), count);
    }

    public async Task<TagRenamePreview> PreviewRenameAsync(Guid tagId, CancellationToken token)
    {
        await using var context = factory.Create(libraryPath);
        var name = await context.Tags.AsNoTracking().Where(tag => tag.Id == tagId).Select(tag => tag.Name).SingleOrDefaultAsync(token);
        return new(tagId, name ?? "", name is null ? [] : await SourceAsync(context, tagId, token));
    }

    public async Task<TagRenameResult> RenameAsync(TagRenamePreview preview, string newName, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(preview);
        TagRenameResult Rejected(TagRenameStatus status) => new(status, preview.TagId, preview.Name, []);
        if (!TagNames.IsValid(newName)) return Rejected(TagRenameStatus.InvalidName);
        newName = newName.Trim();
        await using var context = factory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        var tag = await context.Tags.SingleOrDefaultAsync(tag => tag.Id == preview.TagId, token);
        if (tag is null || tag.Name != preview.Name) return Rejected(TagRenameStatus.Conflict);
        var books = await SourceAsync(context, preview.TagId, token);
        if (books.Length == 0 || !books.Select(book => book.Id).SequenceEqual(preview.Books.Select(book => book.Id).Order()))
            return Rejected(TagRenameStatus.Conflict);
        var normalized = TagNames.Normalize(newName);
        var target = await context.Tags.SingleOrDefaultAsync(other => other.Id != tag.Id && other.NormalizedName == normalized, token);
        if (target is not null)
        {
            var links = await AssignmentSourcesAsync(context, tag.Id, books.Select(book => book.Id).ToArray(), token);
            if (links.Length != books.Length || !await ReplaceAssignmentsAsync(context, tag.Id, target.Id, links, token))
                return Rejected(TagRenameStatus.Conflict);
            context.Tags.Remove(tag);
            await context.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            return new(TagRenameStatus.Renamed, target.Id, target.Name, books);
        }
        if (tag.Name == newName) return Rejected(TagRenameStatus.Unchanged);
        tag.Name = newName;
        tag.NormalizedName = normalized;
        await context.SaveChangesAsync(token);
        var updated = DateTimeOffset.UtcNow;
        foreach (var ids in books.Select(book => book.Id).Chunk(500))
        {
            var changed = await context.Books.Where(book => ids.Contains(book.Id))
                .ExecuteUpdateAsync(setters => setters.SetProperty(book => book.UpdatedUtc, updated), token);
            if (changed != ids.Length) return Rejected(TagRenameStatus.Conflict);
        }
        await transaction.CommitAsync(token);
        return new(TagRenameStatus.Renamed, tag.Id, tag.Name, books);
    }

    private static async Task<TagSourceBook[]> SourceAsync(LibraryDbContext context, Guid tagId, CancellationToken token) =>
        (await context.BookTags.AsNoTracking().Where(link => link.TagId == tagId)
            .Select(link => new TagSourceBook(link.BookId, link.Book.Title)).ToArrayAsync(token)).OrderBy(book => book.Id).ToArray();

    private static void ValidatePage(int skip, int take)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        if (take is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(take));
    }
}

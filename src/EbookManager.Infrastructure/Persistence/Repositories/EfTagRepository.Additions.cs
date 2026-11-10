using EbookManager.Domain.Tags;
using EbookManager.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Infrastructure.Persistence.Repositories;

public sealed partial class EfTagRepository
{
    private static async Task<List<TagSourceBook>?> AddAssignmentsAsync(LibraryDbContext context, string[] names,
        AssignmentSource[] links, CancellationToken token)
    {
        var targets = names.Select(name => name.Trim()).DistinctBy(TagNames.Normalize).ToArray();
        var keys = targets.Select(TagNames.Normalize).ToArray();
        var existing = await context.Tags.AsNoTracking().Where(tag => keys.Contains(tag.NormalizedName))
            .ToDictionaryAsync(tag => tag.NormalizedName, tag => tag.Id, token);
        var targetIds = new List<Guid>(targets.Length);
        foreach (var name in targets)
        {
            var key = TagNames.Normalize(name);
            if (!existing.TryGetValue(key, out var id))
            {
                id = Guid.NewGuid();
                context.Tags.Add(new TagEntity { Id = id, Name = name, NormalizedName = key });
            }
            targetIds.Add(id);
        }
        await context.SaveChangesAsync(token);

        var changed = new List<TagSourceBook>();
        var updated = DateTimeOffset.UtcNow;
        foreach (var batch in links.Chunk(500))
        {
            var bookIds = batch.Select(book => book.BookId).ToArray();
            var alreadyAssigned = (await context.BookTags.AsNoTracking().Where(link => bookIds.Contains(link.BookId) && targetIds.Contains(link.TagId))
                .Select(link => new { link.BookId, link.TagId }).ToArrayAsync(token)).Select(link => (link.BookId, link.TagId)).ToHashSet();
            var lastOrders = await context.BookTags.AsNoTracking().Where(link => bookIds.Contains(link.BookId))
                .GroupBy(link => link.BookId).Select(group => new { Id = group.Key, Order = group.Max(link => link.Order) })
                .ToDictionaryAsync(book => book.Id, book => book.Order, token);
            var changedIds = new List<Guid>();
            foreach (var book in batch)
            {
                var lastOrder = lastOrders[book.BookId];
                var order = lastOrder;
                foreach (var tagId in targetIds)
                {
                    if (alreadyAssigned.Contains((book.BookId, tagId))) continue;
                    context.BookTags.Add(new BookTagEntity { BookId = book.BookId, TagId = tagId, Order = checked(++order) });
                }
                if (order == lastOrder) continue;
                changedIds.Add(book.BookId);
                changed.Add(new(book.BookId, book.Title));
            }
            if (changedIds.Count == 0) continue;
            await context.SaveChangesAsync(token);
            context.ChangeTracker.Clear();
            var count = await context.Books.Where(book => changedIds.Contains(book.Id))
                .ExecuteUpdateAsync(setters => setters.SetProperty(book => book.UpdatedUtc, updated), token);
            if (count != changedIds.Count) return null;
        }
        return changed;
    }
}

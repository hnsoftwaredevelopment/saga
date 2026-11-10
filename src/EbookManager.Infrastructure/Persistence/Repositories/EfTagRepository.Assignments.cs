using EbookManager.Domain.Tags;
using EbookManager.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Infrastructure.Persistence.Repositories;

public sealed partial class EfTagRepository
{
    public async Task<TagAssignmentPreview> PreviewAssignmentsAsync(Guid tagId, IReadOnlyCollection<Guid> bookIds, CancellationToken token)
    {
        await using var context = factory.Create(libraryPath);
        var name = await context.Tags.AsNoTracking().Where(tag => tag.Id == tagId).Select(tag => tag.Name).SingleOrDefaultAsync(token);
        var ids = bookIds.Distinct().ToArray();
        var links = await AssignmentSourcesAsync(context, tagId, ids, token);
        if (links.Length != ids.Length) return new(tagId, name ?? "", []);
        return new(tagId, name ?? "", links.Select(link => new TagSourceBook(link.BookId, link.Title)).ToArray());
    }

    public async Task<TagAssignmentResult> ApplyAssignmentsAsync(TagAssignmentPreview preview, TagAssignmentRequest request, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentNullException.ThrowIfNull(request);
        TagAssignmentResult Rejected(TagAssignmentStatus status) => new(status, preview.TagId, null, null, []);
        if (!Enum.IsDefined(request.Operation) ||
            request.Operation == TagAssignmentOperation.Replace && !TagNames.IsValid(request.TargetName) ||
            request.Operation == TagAssignmentOperation.Split && !TagNames.AreValidSplitTargets(preview.Name, request.TargetNames))
            return Rejected(TagAssignmentStatus.InvalidName);
        var ids = preview.Books.Select(book => book.Id).Order().ToArray();
        if (ids.Length == 0 || ids.Distinct().Count() != ids.Length) return Rejected(TagAssignmentStatus.Conflict);
        await using var context = factory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        var source = await context.Tags.AsNoTracking().SingleOrDefaultAsync(tag => tag.Id == preview.TagId, token);
        if (source is null || source.Name != preview.Name) return Rejected(TagAssignmentStatus.Conflict);
        var links = await AssignmentSourcesAsync(context, source.Id, ids, token);
        if (!ids.SequenceEqual(links.Select(link => link.BookId))) return Rejected(TagAssignmentStatus.Conflict);
        if (request.Operation == TagAssignmentOperation.Split &&
            !preview.Books.OrderBy(book => book.Id).Select(book => book.Title).SequenceEqual(links.Select(link => link.Title)))
            return Rejected(TagAssignmentStatus.Conflict);

        IReadOnlyList<string> names = request.Operation switch
        {
            TagAssignmentOperation.Split => request.TargetNames,
            TagAssignmentOperation.Replace => [request.TargetName!],
            _ => []
        };
        if (names.Any(name => TagNames.Normalize(name) == source.NormalizedName))
            return Rejected(request.Operation == TagAssignmentOperation.Split ? TagAssignmentStatus.InvalidName : TagAssignmentStatus.Unchanged);
        var targets = await ResolveTargetsAsync(context, names, token);
        if (!await ReplaceAssignmentsAsync(context, source.Id, targets.Select(target => target.Id).ToArray(), links, token))
            return Rejected(TagAssignmentStatus.Conflict);
        await transaction.CommitAsync(token);
        var target = targets.Length == 1 ? targets[0] : null;
        return new(TagAssignmentStatus.Applied, source.Id, target?.Id, target?.Name,
            links.Select(link => new TagSourceBook(link.BookId, link.Title)).ToArray());
    }

    private static async Task<TagEntity[]> ResolveTargetsAsync(LibraryDbContext context, IReadOnlyList<string> names, CancellationToken token)
    {
        if (names.Count == 0) return [];
        var normalized = names.Select(TagNames.Normalize).ToArray();
        var existing = await context.Tags.AsNoTracking().Where(tag => normalized.Contains(tag.NormalizedName))
            .ToDictionaryAsync(tag => tag.NormalizedName, token);
        var targets = new List<TagEntity>(names.Count);
        for (var index = 0; index < names.Count; index++)
        {
            var key = normalized[index];
            if (!existing.TryGetValue(key, out var tag))
            {
                tag = new() { Id = Guid.NewGuid(), Name = names[index].Trim(), NormalizedName = key };
                context.Tags.Add(tag);
            }
            targets.Add(tag);
        }
        await context.SaveChangesAsync(token);
        return targets.ToArray();
    }

    private static async Task<bool> ReplaceAssignmentsAsync(LibraryDbContext context, Guid sourceId, Guid[] targetIds,
        AssignmentSource[] links, CancellationToken token)
    {
        var updated = DateTimeOffset.UtcNow;
        foreach (var rows in links.Chunk(500))
        {
            var chunk = rows.Select(link => link.BookId).ToArray();
            var alreadyAssigned = targetIds.Length == 0 ? [] : (await context.BookTags.AsNoTracking()
                .Where(link => targetIds.Contains(link.TagId) && chunk.Contains(link.BookId))
                .Select(link => new { link.BookId, link.TagId }).ToArrayAsync(token))
                .Select(link => (link.BookId, link.TagId)).ToHashSet();
            var lastOrders = targetIds.Length <= 1 ? [] : await context.BookTags.AsNoTracking()
                .Where(link => chunk.Contains(link.BookId)).GroupBy(link => link.BookId)
                .Select(group => new { BookId = group.Key, Order = group.Max(link => link.Order) })
                .ToDictionaryAsync(row => row.BookId, row => row.Order, token);
            // Free the original Order slots before adding replacement links; both unique indexes remain valid.
            var deleted = await context.BookTags.Where(link => link.TagId == sourceId && chunk.Contains(link.BookId)).ExecuteDeleteAsync(token);
            if (deleted != chunk.Length) return false;
            if (targetIds.Length > 0)
            {
                foreach (var link in rows)
                {
                    int? freeOrder = link.Order;
                    var lastOrder = lastOrders.GetValueOrDefault(link.BookId);
                    foreach (var targetId in targetIds)
                    {
                        if (alreadyAssigned.Contains((link.BookId, targetId))) continue;
                        // Reuse the freed slot once, then append without shifting existing tags.
                        context.BookTags.Add(new() { BookId = link.BookId, TagId = targetId, Order = freeOrder ?? checked(++lastOrder) });
                        freeOrder = null;
                    }
                }
                await context.SaveChangesAsync(token);
                // Keep memory and change detection bounded for multi-target batches.
                if (targetIds.Length > 1) context.ChangeTracker.Clear();
            }
            var changed = await context.Books.Where(book => chunk.Contains(book.Id))
                .ExecuteUpdateAsync(setters => setters.SetProperty(book => book.UpdatedUtc, updated), token);
            if (changed != chunk.Length) return false;
        }
        return true;
    }

    private sealed record AssignmentSource(Guid BookId, string Title, int Order);
    private static async Task<AssignmentSource[]> AssignmentSourcesAsync(LibraryDbContext context, Guid tagId, Guid[] ids, CancellationToken token)
    {
        var result = new List<AssignmentSource>();
        foreach (var chunk in ids.Chunk(500))
            result.AddRange(await context.BookTags.AsNoTracking().Where(link => link.TagId == tagId && chunk.Contains(link.BookId))
                .Select(link => new AssignmentSource(link.BookId, link.Book.Title, link.Order)).ToArrayAsync(token));
        return result.OrderBy(link => link.BookId).ToArray();
    }
}

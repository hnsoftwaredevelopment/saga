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
        if (!Enum.IsDefined(request.Operation) || request.Operation == TagAssignmentOperation.Replace && !TagNames.IsValid(request.TargetName))
            return Rejected(TagAssignmentStatus.InvalidName);
        var ids = preview.Books.Select(book => book.Id).Order().ToArray();
        if (ids.Length == 0 || ids.Distinct().Count() != ids.Length) return Rejected(TagAssignmentStatus.Conflict);
        await using var context = factory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        var source = await context.Tags.AsNoTracking().SingleOrDefaultAsync(tag => tag.Id == preview.TagId, token);
        if (source is null || source.Name != preview.Name) return Rejected(TagAssignmentStatus.Conflict);
        var links = await AssignmentSourcesAsync(context, source.Id, ids, token);
        if (!ids.SequenceEqual(links.Select(link => link.BookId))) return Rejected(TagAssignmentStatus.Conflict);

        TagEntity? target = null;
        if (request.Operation == TagAssignmentOperation.Replace)
        {
            var normalized = TagNames.Normalize(request.TargetName!);
            if (normalized == source.NormalizedName) return Rejected(TagAssignmentStatus.Unchanged);
            target = await context.Tags.SingleOrDefaultAsync(tag => tag.NormalizedName == normalized, token);
            if (target is null)
            {
                target = new() { Id = Guid.NewGuid(), Name = request.TargetName!.Trim(), NormalizedName = normalized };
                context.Tags.Add(target);
                await context.SaveChangesAsync(token);
            }
        }
        var updated = DateTimeOffset.UtcNow;
        foreach (var chunk in ids.Chunk(500))
        {
            var alreadyAssigned = target is null ? [] : (await context.BookTags.AsNoTracking()
                .Where(link => link.TagId == target.Id && chunk.Contains(link.BookId)).Select(link => link.BookId).ToArrayAsync(token)).ToHashSet();
            // Free the original Order slots before adding replacement links; both unique indexes remain valid.
            var deleted = await context.BookTags.Where(link => link.TagId == source.Id && chunk.Contains(link.BookId)).ExecuteDeleteAsync(token);
            if (deleted != chunk.Length) return Rejected(TagAssignmentStatus.Conflict);
            if (target is not null)
            {
                var chunkIds = chunk.ToHashSet();
                foreach (var link in links.Where(link => chunkIds.Contains(link.BookId) && !alreadyAssigned.Contains(link.BookId)))
                    context.BookTags.Add(new() { BookId = link.BookId, TagId = target.Id, Order = link.Order });
                await context.SaveChangesAsync(token);
            }
            var changed = await context.Books.Where(book => chunk.Contains(book.Id))
                .ExecuteUpdateAsync(setters => setters.SetProperty(book => book.UpdatedUtc, updated), token);
            if (changed != chunk.Length) return Rejected(TagAssignmentStatus.Conflict);
        }
        await transaction.CommitAsync(token);
        return new(TagAssignmentStatus.Applied, source.Id, target?.Id, target?.Name,
            links.Select(link => new TagSourceBook(link.BookId, link.Title)).ToArray());
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

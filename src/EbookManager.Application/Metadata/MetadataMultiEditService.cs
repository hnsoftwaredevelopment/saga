using EbookManager.Application.Books;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.CustomMetadata;
using EbookManager.Domain.Metadata;

namespace EbookManager.Application.Metadata;

public sealed class MetadataMultiEditService(IBookRepository repository, BookService? bookService,
    ICustomMetadataRepository? customRepository = null)
{
    public async Task<IReadOnlyList<MultiEditItem>> ApplyAsync(IReadOnlyCollection<Guid> ids,
        MetadataMultiEditRequest request, CancellationToken token)
    {
        var results = new List<MultiEditItem>();
        foreach (var id in ids.Distinct())
        {
            if (token.IsCancellationRequested)
            {
                results.Add(new(id, MultiEditStatus.NotProcessed, Failure: MultiEditFailure.Cancelled));
                continue;
            }
            results.Add(await ApplyOneAsync(id, request, token));
        }
        return results;
    }

    private async Task<MultiEditItem> ApplyOneAsync(Guid id, MetadataMultiEditRequest request, CancellationToken token)
    {
        Book? current;
        try { current = await repository.GetAsync(id, token); }
        catch (OperationCanceledException) { return new(id, MultiEditStatus.NotProcessed, Failure: MultiEditFailure.Cancelled); }
        catch (Exception) { return new(id, MultiEditStatus.Failed, Failure: MultiEditFailure.Read); }
        if (current is null) return new(id, MultiEditStatus.NotFound);
        var changes = request.CustomFields ?? [];
        if (changes.Count > 0 && customRepository is null)
            return new(id, MultiEditStatus.Failed, current, MultiEditFailure.CustomUnavailable);
        var proposed = MetadataMultiEditTransform.Apply(current, request);
        var standardChanged = !ReferenceEquals(proposed, current);
        var stored = current;
        var changed = false;
        if (standardChanged)
        {
            try
            {
                await repository.UpdateAsync(proposed, token);
                stored = proposed;
                changed = true;
            }
            catch (BookConflictException) { return new(id, MultiEditStatus.Failed, current, MultiEditFailure.Duplicate); }
            catch (Exception)
            {
                // A provider may throw after commit: inspect persisted state, never replay the operation.
                try
                {
                    stored = await repository.GetAsync(id, CancellationToken.None);
                    if (stored?.Metadata != proposed.Metadata || stored.ReadingStatus != proposed.ReadingStatus)
                        return new(id, MultiEditStatus.Failed, stored, MultiEditFailure.Save);
                    changed = true;
                }
                catch (Exception) { return new(id, MultiEditStatus.Partial, Failure: MultiEditFailure.Uncertain); }
            }
        }
        var customFailed = new List<string>();
        var customUncertain = false;
        if (changes.Count > 0)
        {
            foreach (var change in changes)
            {
                try
                {
                    var values = await customRepository!.GetValuesAsync(id, CancellationToken.None);
                    var previous = values.SingleOrDefault(value => value.FieldId == change.FieldId);
                    if (SameValue(previous, change.Value)) continue;
                    try
                    {
                        if (change.Value is null) await customRepository.DeleteValueAsync(id, change.FieldId, CancellationToken.None);
                        else await customRepository.SetValueAsync(change.Value with { BookId = id, UpdatedUtc = DateTimeOffset.UtcNow }, CancellationToken.None);
                        changed = true;
                    }
                    catch (Exception)
                    {
                        try
                        {
                            var actual = (await customRepository.GetValuesAsync(id, CancellationToken.None)).SingleOrDefault(value => value.FieldId == change.FieldId);
                            if (!SameValue(actual, previous)) changed = true;
                            if (!SameValue(actual, change.Value)) customFailed.Add(change.Name);
                        }
                        catch (Exception) { customUncertain = true; customFailed.Add(change.Name); }
                    }
                }
                catch (Exception) { customFailed.Add(change.Name); }
            }
        }
        var fileWarning = false;
        if (changed && standardChanged && bookService is not null)
        {
            try
            {
                var saved = await bookService.SynchronizeStoredMetadataAsync(id, CancellationToken.None);
                fileWarning = saved.Status != BookSaveStatus.Succeeded || saved.FileResults.Any(file => file.Result.Status == MetadataWriteBackStatus.Failed);
            }
            catch (Exception) { fileWarning = true; }
        }
        if (customFailed.Count > 0)
            return new(id, changed || customUncertain ? MultiEditStatus.Partial : MultiEditStatus.Failed, stored,
                customUncertain ? MultiEditFailure.Uncertain : MultiEditFailure.Custom, customFailed, fileWarning);
        return new(id, fileWarning ? MultiEditStatus.FileWarning : changed ? MultiEditStatus.Updated : MultiEditStatus.Unchanged, stored);
    }

    private static bool SameValue(CustomMetadataValue? left, CustomMetadataValue? right) =>
        left?.TextValue == right?.TextValue && left?.NumberValue == right?.NumberValue &&
        left?.DateValue == right?.DateValue && left?.BooleanValue == right?.BooleanValue;
}

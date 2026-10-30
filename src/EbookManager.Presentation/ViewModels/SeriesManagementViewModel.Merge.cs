using EbookManager.Application.Series;
using EbookManager.Domain.Series;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class SeriesManagementViewModel
{
    private SeriesMergePreview? confirmedPreview;
    public bool CanFindDuplicates => CanBrowse && !IsSearching && mergeService is not null;
    public ISeriesDuplicateIndex CreateDuplicateIndex() => new SeriesDuplicateSearchService(repository);

    public async Task<SeriesMergePreview?> PreviewCandidateAsync(SeriesDuplicateCandidate candidate)
    {
        if (!CanFindDuplicates) return null;
        confirmedPreview = null;
        IsBusy = true;
        try
        {
            var names = new[] { candidate.First.Name, candidate.Second.Name };
            var preview = await Task.Run(() => mergeService!.PreviewAsync(names, lifetime.Token), lifetime.Token);
            if (disposed) return null;
            if (preview is null || preview.Groups.Count != 2 || !names.ToHashSet(SeriesNames.Comparer).SetEquals(preview.Groups.Select(group => group.Name)))
            { SetStatus("SeriesMergeConflict", true); return null; }
            return confirmedPreview = preview;
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception) { if (!disposed) SetStatus("SeriesLoadFailed", true); return null; }
        finally { if (!disposed) IsBusy = false; }
    }

    public async Task<bool> MergeCandidateAsync(SeriesMergePreview preview, string targetName)
    {
        if (!CanFindDuplicates || !ReferenceEquals(confirmedPreview, preview) || !preview.Groups.Any(group => group.Name == targetName)) return false;
        confirmedPreview = null;
        IsBusy = true;
        ++searchVersion;
        IsSearching = false;
        Warnings = [];
        SetStatus("SeriesMergeWorking", false);
        var committed = false;
        try
        {
            var completion = await Task.Run(() => mergeService!.MergeAsync(preview, targetName, lifetime.Token), lifetime.Token);
            committed = completion.Change.Status == SeriesMergeStatus.Merged;
            if (committed) changedBookIds.UnionWith(completion.Change.Books.Select(book => book.Id));
            if (disposed) return committed;
            Warnings = completion.Warnings;
            if (!committed) { SetStatus("SeriesMergeConflict", true); return false; }
            Name = completion.Change.Name;
            Books = [];
            TotalBooks = preview.BookCount;
            // Clear the manager filter after merging, while retaining the destination's details.
            if (!await SearchCoreAsync("")) throw new InvalidOperationException("Series index refresh failed.");
            if (disposed) return true;
            var page = await Task.Run(() => repository.ListBooksAsync(Name, 0, 200, lifetime.Token), lifetime.Token);
            if (disposed) return true;
            Books = page.Books;
            TotalBooks = page.TotalCount;
            StatusMessage = string.Format(CultureInfo.CurrentCulture, localize(Warnings.Count == 0 ? "SeriesMergeSaved" : "SeriesMergeSavedWithWarnings"), completion.Change.Books.Count, Warnings.Count);
            IsError = Warnings.Count > 0;
            NotifySelection();
            return true;
        }
        catch (OperationCanceledException) { return committed; }
        catch (Exception) { if (!disposed) SetStatus(committed ? "SeriesMergeRefreshFailed" : "SeriesMergeFailed", true); return committed; }
        finally { if (!disposed) IsBusy = false; }
    }
}

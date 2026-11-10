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
        if (!CanFindDuplicates || !ReferenceEquals(confirmedPreview, preview) || string.IsNullOrWhiteSpace(targetName) || (!preview.AllowNewName && !preview.Groups.Any(group => group.Name == targetName))) return false;
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
            if (!committed) { SetStatus(completion.Change.Status == SeriesMergeStatus.NameExists ? "SeriesManualMergeNameExists" : "SeriesMergeConflict", true); return false; }
            await RefreshSeriesDestinationAsync(completion.Change.Name, preview.BookCount);
            if (disposed) return true;
            StatusMessage = string.Format(CultureInfo.CurrentCulture, localize(Warnings.Count == 0 ? "SeriesMergeSaved" : "SeriesMergeSavedWithWarnings"), completion.Change.Books.Count, Warnings.Count);
            IsError = Warnings.Count > 0;
            NotifySelection();
            return true;
        }
        catch (OperationCanceledException) { return committed; }
        catch (Exception) { if (!disposed) SetStatus(committed ? "SeriesMergeRefreshFailed" : "SeriesMergeFailed", true); return committed; }
        finally { if (!disposed) IsBusy = false; }
    }

    private async Task RefreshSeriesDestinationAsync(string name, int bookCount)
    {
        SetNumberSelection([]);
        SelectedBook = null;
        Name = name;
        Books = [];
        TotalBooks = bookCount;
        if (!await SearchCoreAsync("")) throw new InvalidOperationException("Series index refresh failed.");
        if (disposed) return;
        var page = await Task.Run(() => repository.ListBooksAsync(name, 0, 200, lifetime.Token), lifetime.Token);
        if (disposed) return;
        Books = page.Books;
        TotalBooks = page.TotalCount;
        // Keep a late-sorting destination visible without consuming additional repository rows.
        if (!Series.Any(row => SeriesNames.Same(row.Name, name)))
        {
            Series = Series.Append(new SeriesSummary(name, TotalBooks)).OrderBy(row => row.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(row => row.Name, StringComparer.Ordinal).ToArray();
            OnPropertyChanged(nameof(HasMoreSeries));
        }
        SetMergeSeries(Series.Where(row => SeriesNames.Same(row.Name, name)));
    }
}

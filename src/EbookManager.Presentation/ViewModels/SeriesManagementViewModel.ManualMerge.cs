using EbookManager.Domain.Series;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class SeriesManagementViewModel
{
    private readonly HashSet<string> mergeSeriesNames = new(SeriesNames.Comparer);
    public IReadOnlyCollection<string> MergeSeriesNames => mergeSeriesNames.ToArray();
    public int MergeSeriesCount => mergeSeriesNames.Count;
    public string MergeSeriesSummary => string.Format(CultureInfo.CurrentCulture, localize("SeriesManualMergeSelected"), MergeSeriesCount);
    public bool CanMergeSelectedSeries => CanBrowse && !IsSearching && mergeService is not null && MergeSeriesCount >= 2;
    public bool CanChooseSeries => CanBrowse && !IsSearching;
    public bool CanBrowseBooks => CanBrowse && MergeSeriesCount <= 1;
    public void SetMergeSeries(IEnumerable<SeriesSummary> selected)
    {
        var loaded = Series.Select(row => row.Name.Trim()).ToHashSet(SeriesNames.Comparer);
        var names = selected.Select(row => row.Name.Trim()).Where(loaded.Contains).ToHashSet(SeriesNames.Comparer);
        if (mergeSeriesNames.SetEquals(names)) return;
        mergeSeriesNames.Clear(); mergeSeriesNames.UnionWith(names); confirmedPreview = null;
        foreach (var property in new[] { nameof(MergeSeriesNames), nameof(MergeSeriesCount), nameof(MergeSeriesSummary), nameof(CanMergeSelectedSeries),
            nameof(CanBrowseBooks), nameof(CanRename), nameof(CanEditNumber), nameof(CanAssignNumbers), nameof(HasMoreBooks) }) OnPropertyChanged(property);
    }
    public async Task<SeriesMergePreview?> PreviewSelectedSeriesAsync()
    {
        if (!CanMergeSelectedSeries) return null;
        var names = Series.Where(row => mergeSeriesNames.Contains(row.Name.Trim())).Select(row => row.Name).ToArray();
        confirmedPreview = null; IsBusy = true;
        try
        {
            var preview = await Task.Run(() => mergeService!.PreviewAsync(names, lifetime.Token), lifetime.Token);
            if (disposed) return null;
            if (preview is null || !mergeSeriesNames.SetEquals(names) || !names.ToHashSet(SeriesNames.Comparer).SetEquals(preview.Groups.Select(group => group.Name)))
            { SetStatus("SeriesMergeConflict", true); return null; }
            return confirmedPreview = preview with { AllowNewName = true };
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception) { if (!disposed) SetStatus("SeriesLoadFailed", true); return null; }
        finally { if (!disposed) IsBusy = false; }
    }
}

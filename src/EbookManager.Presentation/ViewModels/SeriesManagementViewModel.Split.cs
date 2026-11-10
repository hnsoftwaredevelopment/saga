using EbookManager.Domain.Series;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class SeriesManagementViewModel
{
    private SeriesRenamePreview? splitPreview;

    public bool CanSplitSeries => CanBrowse && !IsSearching && MergeSeriesCount == 1 &&
        SelectedNumberBooks.Count > 0 && SelectedNumberBooks.Count < TotalBooks;

    public async Task<SeriesRenamePreview?> PreviewSplitAsync()
    {
        if (!CanSplitSeries) return null;
        var ids = SelectedNumberBooks.Select(book => book.Id).ToArray();
        splitPreview = null;
        IsBusy = true;
        try
        {
            var preview = await Task.Run(() => renameService.PreviewAsync(Name, lifetime.Token), lifetime.Token);
            if (disposed) return null;
            var sourceIds = preview.Books.Select(book => book.Id).ToHashSet();
            if (!SeriesNames.Same(preview.Name, Name) || ids.Length >= preview.Books.Count || !sourceIds.IsSupersetOf(ids))
            {
                SetStatus("SeriesSplitConflict", true);
                return null;
            }
            return splitPreview = preview with { SelectedBookIds = Array.AsReadOnly(ids) };
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception) { if (!disposed) SetStatus("SeriesLoadFailed", true); return null; }
        finally { if (!disposed) IsBusy = false; }
    }
}

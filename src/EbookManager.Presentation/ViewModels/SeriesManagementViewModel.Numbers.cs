using EbookManager.Domain.Series;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class SeriesManagementViewModel
{
    private SeriesNumbersPreview? numbersPreview;
    public IReadOnlyList<SeriesBook> SelectedNumberBooks { get; private set; } = [];
    public bool CanAssignNumbers => CanBrowse && !IsSearching && numbersService is not null && SelectedNumberBooks.Count >= 2;
    public void SetNumberSelection(IEnumerable<SeriesBook> selected)
    {
        var ids = selected.Select(book => book.Id).ToHashSet();
        var rows = Books.Where(book => ids.Contains(book.Id)).ToArray();
        if (SelectedNumberBooks.SequenceEqual(rows)) return;
        SelectedNumberBooks = rows; numbersPreview = null;
        OnPropertyChanged(nameof(SelectedNumberBooks)); OnPropertyChanged(nameof(CanAssignNumbers)); OnPropertyChanged(nameof(CanEditNumber));
    }
    public async Task<SeriesNumbersPreview?> PreviewNumbersAsync()
    {
        if (!CanAssignNumbers) return null;
        var ids = SelectedNumberBooks.Select(book => book.Id).ToArray(); numbersPreview = null;
        IsBusy = true;
        try
        {
            var preview = await Task.Run(() => numbersService!.PreviewAsync(Name, ids, lifetime.Token), lifetime.Token);
            if (disposed) return null;
            if (preview is null || !SeriesNames.Same(preview.Name, Name) || !preview.Books.Select(book => book.Book.Id).SequenceEqual(SelectedNumberBooks.Select(book => book.Id)))
            { SetStatus("SeriesNumbersConflict", true); return null; }
            return numbersPreview = preview;
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception) { if (!disposed) SetStatus("SeriesLoadFailed", true); return null; }
        finally { if (!disposed) IsBusy = false; }
    }
    public async Task<bool> AssignNumbersAsync(SeriesNumbersPreview preview, IReadOnlyList<SeriesNumberAssignment> assignments)
    {
        if (!CanAssignNumbers || !ReferenceEquals(numbersPreview, preview) || !SeriesNames.Same(Name, preview.Name)) return false;
        numbersPreview = null; IsBusy = true; Warnings = []; SetStatus("SeriesNumbersWorking", false);
        var committed = false; var selectedIds = SelectedNumberBooks.Select(book => book.Id).ToHashSet();
        try
        {
            var completion = await Task.Run(() => numbersService!.ChangeAsync(preview, assignments, lifetime.Token), lifetime.Token);
            committed = completion.Change.Status == SeriesNumberStatus.Changed;
            if (committed) changedBookIds.UnionWith(completion.Change.Books.Select(book => book.Id));
            if (disposed) return committed;
            if (!committed)
            { SetStatus(completion.Change.Status == SeriesNumberStatus.Unchanged ? "SeriesNumberUnchanged" : "SeriesNumbersConflict", completion.Change.Status != SeriesNumberStatus.Unchanged); return false; }
            Warnings = completion.Warnings;
            var take = Math.Max(200, Books.Count); var loaded = new List<SeriesBook>(); SeriesBookPage page;
            do
            {
                page = await Task.Run(() => repository.ListBooksAsync(Name, loaded.Count, 200, lifetime.Token), lifetime.Token);
                loaded.AddRange(page.Books);
            } while (loaded.Count < take && loaded.Count < page.TotalCount && page.Books.Count > 0);
            if (disposed) return true;
            Books = loaded; TotalBooks = page.TotalCount;
            SetNumberSelection(Books.Where(book => selectedIds.Contains(book.Id)));
            SelectedBook = SelectedNumberBooks.FirstOrDefault();
            StatusMessage = string.Format(CultureInfo.CurrentCulture, localize("SeriesNumbersSaved"), completion.Change.Books.Count);
            if (completion.Change.MatchingOtherBooks > 0) StatusMessage += "\n" + string.Format(CultureInfo.CurrentCulture, localize("SeriesNumbersDuplicates"), completion.Change.MatchingOtherBooks);
            if (SelectedNumberBooks.Count != selectedIds.Count) StatusMessage += "\n" + localize("SeriesNumbersOutside");
            if (Warnings.Count > 0) StatusMessage += "\n" + string.Format(CultureInfo.CurrentCulture, localize("SeriesNumbersWarnings"), Warnings.Count);
            IsError = Warnings.Count > 0; NotifySelection(); return true;
        }
        catch (OperationCanceledException) { return committed; }
        catch (Exception) { if (!disposed) SetStatus(committed ? "SeriesNumbersRefreshFailed" : "SeriesNumbersFailed", true); return committed; }
        finally { if (!disposed) IsBusy = false; }
    }
}

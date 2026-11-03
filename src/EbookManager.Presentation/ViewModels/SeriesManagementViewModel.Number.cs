using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Domain.Series;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class SeriesManagementViewModel
{
    private SeriesNumberPreview? numberPreview;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanEditNumber))] private SeriesBook? selectedBook;
    public bool CanEditNumber => CanBrowse && !IsSearching && numberService is not null && SelectedBook is not null && Books.Contains(SelectedBook);
    partial void OnSelectedBookChanged(SeriesBook? value) => numberPreview = null;

    public async Task<SeriesNumberPreview?> PreviewNumberAsync()
    {
        if (!CanEditNumber) return null;
        var id = SelectedBook!.Id;
        numberPreview = null;
        IsBusy = true;
        try
        {
            var preview = await Task.Run(() => numberService!.PreviewAsync(id, lifetime.Token), lifetime.Token);
            if (disposed) return null;
            if (preview is null || preview.Book.Id != SelectedBook?.Id || !SeriesNames.Same(preview.StoredName, Name))
            { SetStatus("SeriesNumberConflict", true); return null; }
            return numberPreview = preview;
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception) { if (!disposed) SetStatus("SeriesLoadFailed", true); return null; }
        finally { if (!disposed) IsBusy = false; }
    }

    public async Task<bool> ChangeNumberAsync(SeriesNumberPreview preview, decimal? number)
    {
        if (!CanEditNumber || !ReferenceEquals(numberPreview, preview) || preview.Book.Id != SelectedBook!.Id || !SeriesNames.Same(preview.StoredName, Name)) return false;
        numberPreview = null;
        IsBusy = true;
        Warnings = [];
        SetStatus("SeriesNumberWorking", false);
        var committed = false;
        try
        {
            var completion = await Task.Run(() => numberService!.ChangeAsync(preview, number, lifetime.Token), lifetime.Token);
            committed = completion.Change.Status == SeriesNumberStatus.Changed;
            if (committed) changedBookIds.Add(preview.Book.Id);
            if (disposed) return committed;
            if (!committed)
            { SetStatus(completion.Change.Status == SeriesNumberStatus.Unchanged ? "SeriesNumberUnchanged" : "SeriesNumberConflict", completion.Change.Status != SeriesNumberStatus.Unchanged); return false; }
            if (completion.FileWarning) Warnings = [new(preview.Book.Id, preview.StoredName, preview.Book.Title)];
            // Reload only the prefix the user already loaded; a changed number can move a book beyond it.
            var take = Math.Max(200, Books.Count);
            var loaded = new List<SeriesBook>();
            SeriesBookPage page;
            do
            {
                page = await Task.Run(() => repository.ListBooksAsync(Name, loaded.Count, 200, lifetime.Token), lifetime.Token);
                loaded.AddRange(page.Books);
            } while (loaded.Count < take && loaded.Count < page.TotalCount && page.Books.Count > 0);
            if (disposed) return true;
            Books = loaded;
            TotalBooks = page.TotalCount;
            SelectedBook = Books.FirstOrDefault(book => book.Id == preview.Book.Id);
            StatusMessage = number.HasValue
                ? string.Format(CultureInfo.CurrentCulture, localize("SeriesNumberSaved"), preview.Book.Title, number.Value)
                : string.Format(CultureInfo.CurrentCulture, localize("SeriesNumberCleared"), preview.Book.Title);
            if (completion.Change.MatchingOtherBooks > 0) StatusMessage += "\n" + string.Format(CultureInfo.CurrentCulture, localize("SeriesNumberDuplicate"), completion.Change.MatchingOtherBooks);
            if (SelectedBook is null) StatusMessage += "\n" + localize("SeriesNumberOutsidePage");
            if (completion.FileWarning) StatusMessage += "\n" + localize("SeriesNumberFileWarning");
            IsError = completion.FileWarning;
            NotifySelection();
            return true;
        }
        catch (OperationCanceledException) { return committed; }
        catch (Exception) { if (!disposed) SetStatus(committed ? "SeriesNumberRefreshFailed" : "SeriesNumberFailed", true); return committed; }
        finally { if (!disposed) IsBusy = false; }
    }
}

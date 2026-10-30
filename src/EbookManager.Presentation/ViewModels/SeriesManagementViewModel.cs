using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Application.Series;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Series;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class SeriesManagementViewModel(ISeriesRepository repository, ISeriesRenameService renameService,
    Func<string, string> localize, ISeriesMergeService? mergeService = null, ISeriesNumberService? numberService = null) : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource lifetime = new();
    private readonly HashSet<Guid> changedBookIds = [];
    private int searchVersion;
    private bool disposed;
    [ObservableProperty] private string query = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private IReadOnlyList<SeriesSummary> series = [];
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanEditNumber))] private IReadOnlyList<SeriesBook> books = [];
    [ObservableProperty] private IReadOnlyList<SeriesSourceBook> warnings = [];
    [ObservableProperty] private int totalSeries;
    [ObservableProperty] private int totalBooks;
    [ObservableProperty] private string statusMessage = "";
    [ObservableProperty] private bool isError;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanBrowse))]
    [NotifyPropertyChangedFor(nameof(CanRename))]
    [NotifyPropertyChangedFor(nameof(CanFindDuplicates))]
    [NotifyPropertyChangedFor(nameof(CanEditNumber))]
    [NotifyPropertyChangedFor(nameof(IsWorking))]
    [NotifyPropertyChangedFor(nameof(HasMoreBooks))]
    [NotifyPropertyChangedFor(nameof(HasMoreSeries))]
    private bool isBusy;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditNumber))]
    [NotifyPropertyChangedFor(nameof(CanFindDuplicates))]
    [NotifyPropertyChangedFor(nameof(IsWorking))]
    [NotifyPropertyChangedFor(nameof(HasMoreSeries))]
    private bool isSearching;

    public IReadOnlyCollection<Guid> ChangedBookIds => changedBookIds.ToArray();
    public bool CanBrowse => !disposed && !IsBusy;
    public bool CanRename => CanBrowse && !string.IsNullOrEmpty(Name) && TotalBooks > 0;
    public bool IsWorking => IsBusy || IsSearching;
    public bool HasMoreSeries => CanBrowse && !IsSearching && Series.Count < TotalSeries;
    public bool HasMoreBooks => CanBrowse && Books.Count < TotalBooks;

    public Task SearchAsync(string text, bool append = false) => CanBrowse ? SearchCoreAsync(text, append) : Task.CompletedTask;

    private async Task<bool> SearchCoreAsync(string text, bool append = false)
    {
        var version = ++searchVersion;
        Query = text;
        IsSearching = true;
        try
        {
            var page = await Task.Run(() => repository.SearchAsync(text, append ? Series.Count : 0, 200, lifetime.Token), lifetime.Token);
            if (disposed || version != searchVersion) return false;
            Series = append ? Series.Concat(page.Series).ToArray() : page.Series;
            TotalSeries = page.TotalCount;
            OnPropertyChanged(nameof(HasMoreSeries));
            return true;
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!disposed && version == searchVersion) SetStatus("SeriesLoadFailed", true); }
        finally { if (!disposed && version == searchVersion) IsSearching = false; }
        return false;
    }

    public async Task SelectAsync(SeriesSummary selected)
    {
        if (!CanBrowse) return;
        IsBusy = true;
        Name = selected.Name;
        SelectedBook = null;
        Books = [];
        TotalBooks = 0;
        Warnings = [];
        SetStatus("", false);
        try
        {
            var page = await Task.Run(() => repository.ListBooksAsync(selected.Name, 0, 200, lifetime.Token), lifetime.Token);
            if (disposed) return;
            Books = page.Books;
            TotalBooks = page.TotalCount;
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!disposed) SetStatus("SeriesLoadFailed", true); }
        finally { if (!disposed) { IsBusy = false; NotifySelection(); } }
    }

    public async Task LoadMoreBooksAsync()
    {
        if (!HasMoreBooks) return;
        IsBusy = true;
        try
        {
            var page = await Task.Run(() => repository.ListBooksAsync(Name, Books.Count, 200, lifetime.Token), lifetime.Token);
            if (disposed) return;
            Books = Books.Concat(page.Books).ToArray();
            TotalBooks = page.TotalCount;
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!disposed) SetStatus("SeriesLoadFailed", true); }
        finally { if (!disposed) IsBusy = false; }
    }

    public async Task<SeriesRenamePreview?> PreviewRenameAsync()
    {
        if (!CanRename) return null;
        IsBusy = true;
        try
        {
            var preview = await Task.Run(() => renameService.PreviewAsync(Name, lifetime.Token), lifetime.Token);
            if (disposed) return null;
            if (preview.Books.Count == 0) { SetStatus("SeriesRenameConflict", true); return null; }
            return preview;
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception) { if (!disposed) SetStatus("SeriesLoadFailed", true); return null; }
        finally { if (!disposed) IsBusy = false; }
    }

    public async Task<bool> RenameAsync(SeriesRenamePreview preview, string newName)
    {
        if (!CanRename || !SeriesNames.Same(preview.Name, Name)) return false;
        IsBusy = true;
        ++searchVersion;
        IsSearching = false;
        Warnings = [];
        SetStatus("SeriesRenameWorking", false);
        var committed = false;
        try
        {
            var result = await Task.Run(() => renameService.RenameAsync(preview, newName, lifetime.Token), lifetime.Token);
            committed = result.Change.Status == SeriesRenameStatus.Renamed;
            if (committed) changedBookIds.UnionWith(result.Change.Books.Select(book => book.Id));
            if (disposed) return false;
            if (result.Change.Status != SeriesRenameStatus.Renamed)
            {
                SetStatus(result.Change.Status switch
                {
                    SeriesRenameStatus.NameExists => "SeriesRenameNameExists",
                    SeriesRenameStatus.InvalidName => "SeriesRenameInvalid",
                    SeriesRenameStatus.Unchanged => "SeriesRenameUnchanged",
                    _ => "SeriesRenameConflict"
                }, result.Change.Status != SeriesRenameStatus.Unchanged);
                return false;
            }
            Name = result.Change.Name;
            TotalBooks = result.Change.Books.Count;
            // Only the name changed; keep the already sorted book pages and their scroll position.
            Warnings = result.Warnings;
            StatusMessage = string.Format(CultureInfo.CurrentCulture, localize(Warnings.Count == 0 ? "SeriesRenameSaved" : "SeriesRenameSavedWithWarnings"), TotalBooks, Warnings.Count);
            IsError = Warnings.Count > 0;
            await SearchCoreAsync(Name);
            NotifySelection();
            return true;
        }
        catch (OperationCanceledException) { return false; }
        catch (Exception)
        {
            if (!disposed) SetStatus(committed ? "SeriesRenameRefreshFailed" : "SeriesRenameFailed", true);
            return committed;
        }
        finally { if (!disposed) IsBusy = false; }
    }

    private void SetStatus(string key, bool error) { StatusMessage = key.Length == 0 ? "" : localize(key); IsError = error; }
    private void NotifySelection()
    {
        OnPropertyChanged(nameof(CanRename));
        OnPropertyChanged(nameof(HasMoreBooks));
        OnPropertyChanged(nameof(Name));
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        ++searchVersion;
        lifetime.Cancel();
        lifetime.Dispose();
    }
}

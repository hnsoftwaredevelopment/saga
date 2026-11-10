using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Application.Series;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Series;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class SeriesManagementViewModel(ISeriesRepository repository, ISeriesRenameService renameService,
    Func<string, string> localize, ISeriesMergeService? mergeService = null, ISeriesNumberService? numberService = null,
    ISeriesNumbersService? numbersService = null) : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource lifetime = new();
    private readonly HashSet<Guid> changedBookIds = [];
    private int searchVersion;
    // Number of repository rows consumed, excluding a merged destination pinned into the visible index.
    private int seriesPageOffset;
    private bool disposed;
    [ObservableProperty] private string query = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private IReadOnlyList<SeriesSummary> series = [];
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanEditNumber))] private IReadOnlyList<SeriesBook> books = [];
    [ObservableProperty] private IReadOnlyList<SeriesSourceBook> warnings = [];
    [ObservableProperty] private int totalSeries;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanSplitSeries))] private int totalBooks;
    [ObservableProperty] private string statusMessage = "";
    [ObservableProperty] private bool isError;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanBrowse))]
    [NotifyPropertyChangedFor(nameof(CanRename))]
    [NotifyPropertyChangedFor(nameof(CanFindDuplicates))]
    [NotifyPropertyChangedFor(nameof(CanEditNumber))]
    [NotifyPropertyChangedFor(nameof(CanAssignNumbers))]
    [NotifyPropertyChangedFor(nameof(CanSplitSeries))]
    [NotifyPropertyChangedFor(nameof(CanMergeSelectedSeries))]
    [NotifyPropertyChangedFor(nameof(CanBrowseBooks))]
    [NotifyPropertyChangedFor(nameof(CanChooseSeries))]
    [NotifyPropertyChangedFor(nameof(IsWorking))]
    [NotifyPropertyChangedFor(nameof(HasMoreBooks))]
    [NotifyPropertyChangedFor(nameof(HasMoreSeries))]
    private bool isBusy;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditNumber))]
    [NotifyPropertyChangedFor(nameof(CanAssignNumbers))]
    [NotifyPropertyChangedFor(nameof(CanSplitSeries))]
    [NotifyPropertyChangedFor(nameof(CanMergeSelectedSeries))]
    [NotifyPropertyChangedFor(nameof(CanChooseSeries))]
    [NotifyPropertyChangedFor(nameof(CanFindDuplicates))]
    [NotifyPropertyChangedFor(nameof(IsWorking))]
    [NotifyPropertyChangedFor(nameof(HasMoreSeries))]
    private bool isSearching;

    public IReadOnlyCollection<Guid> ChangedBookIds => changedBookIds.ToArray();
    public bool CanBrowse => !disposed && !IsBusy;
    public bool CanRename => CanBrowseBooks && !string.IsNullOrEmpty(Name) && TotalBooks > 0;
    public bool IsWorking => IsBusy || IsSearching;
    public bool HasMoreSeries => CanBrowse && !IsSearching && Series.Count < TotalSeries;
    public bool HasMoreBooks => CanBrowseBooks && Books.Count < TotalBooks;

    public Task SearchAsync(string text, bool append = false) => CanBrowse ? SearchCoreAsync(text, append) : Task.CompletedTask;

    private async Task<bool> SearchCoreAsync(string text, bool append = false)
    {
        if (!append) SetMergeSeries([]);
        var version = ++searchVersion;
        Query = text;
        IsSearching = true;
        var skip = append ? seriesPageOffset : 0;
        try
        {
            var page = await Task.Run(() => repository.SearchAsync(text, skip, 200, lifetime.Token), lifetime.Token);
            if (disposed || version != searchVersion) return false;
            seriesPageOffset = skip + page.Series.Count;
            Series = append ? page.Series.Concat(Series).DistinctBy(row => row.Name, SeriesNames.Comparer)
                .OrderBy(row => row.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(row => row.Name, StringComparer.Ordinal).ToArray() : page.Series;
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
        SetMergeSeries([selected]);
        Name = selected.Name;
        SelectedBook = null;
        SetNumberSelection([]);
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

    public async Task<bool> RenameAsync(SeriesRenamePreview preview, string newName, SeriesMoveDestination? destination = null)
    {
        if (!CanRename || !SeriesNames.Same(preview.Name, Name)) return false;
        var split = preview.SelectedBookIds is not null;
        if (preview.SelectedBookIds is { } ids && (!CanSplitSeries || !ReferenceEquals(splitPreview, preview) ||
            !SelectedNumberBooks.Select(book => book.Id).SequenceEqual(ids))) return false;
        if (destination is not null) preview = preview with { Destination = destination };
        splitPreview = null;
        IsBusy = true;
        ++searchVersion;
        IsSearching = false;
        Warnings = [];
        SetStatus(split ? "SeriesSplitWorking" : "SeriesRenameWorking", false);
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
                    SeriesRenameStatus.NameExists => split ? "SeriesMoveNameExists" : "SeriesRenameNameExists",
                    SeriesRenameStatus.InvalidName => "SeriesRenameInvalid",
                    SeriesRenameStatus.Unchanged => "SeriesRenameUnchanged",
                    _ => split ? "SeriesSplitConflict" : "SeriesRenameConflict"
                }, result.Change.Status != SeriesRenameStatus.Unchanged);
                return false;
            }
            Warnings = result.Warnings;
            if (split)
                await RefreshSeriesDestinationAsync(result.Change.Name, result.Change.Books.Count);
            else
            {
                Name = result.Change.Name;
                TotalBooks = result.Change.Books.Count;
                // Only the name changed; keep the already sorted book pages and their scroll position.
                if (!await SearchCoreAsync(Name)) throw new InvalidOperationException("Series index refresh failed.");
                SetMergeSeries(Series.Where(row => SeriesNames.Same(row.Name, Name)));
            }
            if (disposed) return true;
            var savedKey = split ? "SeriesSplitSaved" : "SeriesRenameSaved";
            StatusMessage = string.Format(CultureInfo.CurrentCulture, localize(Warnings.Count == 0 ? savedKey : savedKey + "WithWarnings"), result.Change.Books.Count, Warnings.Count);
            IsError = Warnings.Count > 0;
            NotifySelection();
            return true;
        }
        catch (OperationCanceledException) { return committed; }
        catch (Exception)
        {
            var operation = split ? "SeriesSplit" : "SeriesRename";
            if (!disposed) SetStatus(operation + (committed ? "RefreshFailed" : "Failed"), true);
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

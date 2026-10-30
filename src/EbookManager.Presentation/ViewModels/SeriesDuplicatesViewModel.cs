using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Application.Series;
using EbookManager.Domain.Series;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed record SeriesDuplicateRow(SeriesDuplicateCandidate Candidate, string Reason)
{
    public SeriesSummary First => Candidate.First;
    public SeriesSummary Second => Candidate.Second;
}

public sealed partial class SeriesDuplicatesViewModel(ISeriesDuplicateIndex index, Func<string, string> localize) : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? search;
    private IReadOnlyList<SeriesSummary>? seriesIndex;
    private int version;
    private bool disposed;
    private string query = "";
    [ObservableProperty] private IReadOnlyList<SeriesDuplicateRow> candidates = [];
    [ObservableProperty] private int totalSeries;
    [ObservableProperty] private string statusMessage = "";
    [ObservableProperty] private bool isError;
    [ObservableProperty] private string lastActionMessage = "";
    [ObservableProperty] private bool lastActionIsError;
    [ObservableProperty] private IReadOnlyList<SeriesSourceBook> warnings = [];
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanCompare))] private SeriesDuplicateRow? selected;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsWorking))] [NotifyPropertyChangedFor(nameof(CanBrowse))] [NotifyPropertyChangedFor(nameof(CanSearch))] [NotifyPropertyChangedFor(nameof(CanCompare))] private bool isComparing;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsWorking))] [NotifyPropertyChangedFor(nameof(CanBrowse))] [NotifyPropertyChangedFor(nameof(CanCompare))] private bool isLoading;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsWorking))] [NotifyPropertyChangedFor(nameof(CanBrowse))] [NotifyPropertyChangedFor(nameof(CanCompare))] private bool isSearching;
    public bool IsWorking => IsLoading || IsSearching || IsComparing;
    public bool CanBrowse => !disposed && !IsWorking;
    public bool CanSearch => !disposed && !IsComparing;
    public string Query => query;
    public bool CanCompare => !disposed && !IsWorking && Selected is not null && Candidates.Contains(Selected);

    public async Task ReloadAsync(string? searchText = null)
    {
        if (disposed || IsLoading) return;
        if (searchText is not null) query = searchText;
        search?.Cancel();
        var current = ++version;
        IsLoading = true;
        IsSearching = false;
        seriesIndex = null;
        Selected = null;
        Candidates = [];
        StatusMessage = localize("SeriesDuplicatesLoading");
        IsError = false;
        try
        {
            var loaded = await Task.Run(() => index.LoadAsync(lifetime.Token), lifetime.Token);
            if (disposed || current != version) return;
            seriesIndex = loaded;
            TotalSeries = loaded.Count;
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!disposed) { StatusMessage = localize("SeriesDuplicatesLoadFailed"); IsError = true; } }
        finally { if (!disposed && current == version) IsLoading = false; }
        if (!disposed && seriesIndex is not null) await SearchAsync(query);
    }

    public async Task SearchAsync(string text)
    {
        if (disposed) return;
        query = text;
        if (seriesIndex is null) return;
        var snapshot = seriesIndex;
        search?.Cancel();
        search?.Dispose();
        var cancellation = search = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        var token = cancellation.Token;
        var current = ++version;
        IsSearching = true;
        Selected = null;
        IsError = false;
        try
        {
            var found = await Task.Run(() => SeriesDuplicateFinder.Find(snapshot, text, token: token), token);
            if (disposed || current != version) return;
            Candidates = found.Candidates.Select(candidate => new SeriesDuplicateRow(candidate,
                localize("SeriesDuplicatesFormatting"))).ToArray();
            StatusMessage = found.Truncated ? localize("SeriesDuplicatesLimit") : Candidates.Count == 0
                ? localize("SeriesDuplicatesNone") : string.Format(CultureInfo.CurrentCulture, localize("SeriesDuplicatesCount"), Candidates.Count, TotalSeries);
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!disposed && current == version) { StatusMessage = localize("SeriesDuplicatesLoadFailed"); IsError = true; Candidates = []; } }
        finally { if (!disposed && current == version) IsSearching = false; }
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        ++version;
        lifetime.Cancel(); search?.Cancel(); search?.Dispose(); lifetime.Dispose();
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Application.Authors;
using EbookManager.Domain.Authors;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed record AuthorDuplicateRow(AuthorDuplicateCandidate Candidate, string Reason)
{
    public AuthorSummary First => Candidate.First;
    public AuthorSummary Second => Candidate.Second;
}

public sealed partial class AuthorDuplicatesViewModel(IAuthorDuplicateIndex index, Func<string, string> localize) : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? search;
    private IReadOnlyList<AuthorSummary>? authors;
    private int version;
    private bool disposed;
    private string query = "";
    [ObservableProperty] private IReadOnlyList<AuthorDuplicateRow> candidates = [];
    [ObservableProperty] private int totalAuthors;
    [ObservableProperty] private string statusMessage = "";
    [ObservableProperty] private bool isError;
    [ObservableProperty] private string lastActionMessage = "";
    [ObservableProperty] private bool lastActionIsError;
    [ObservableProperty] private IReadOnlyList<AuthorBook> warnings = [];
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanCompare))] private AuthorDuplicateRow? selected;
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
        authors = null;
        Selected = null;
        Candidates = [];
        StatusMessage = localize("AuthorDuplicatesLoading");
        IsError = false;
        try
        {
            var loaded = await Task.Run(() => index.LoadAsync(lifetime.Token), lifetime.Token);
            if (disposed || current != version) return;
            authors = loaded;
            TotalAuthors = loaded.Count;
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!disposed) { StatusMessage = localize("AuthorDuplicatesLoadFailed"); IsError = true; } }
        finally { if (!disposed && current == version) IsLoading = false; }
        if (!disposed && authors is not null) await SearchAsync(query);
    }

    public async Task SearchAsync(string text)
    {
        if (disposed) return;
        query = text;
        if (authors is null) return;
        var snapshot = authors;
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
            var found = await Task.Run(() => AuthorDuplicateFinder.Find(snapshot, text, token: token), token);
            if (disposed || current != version) return;
            Candidates = found.Candidates.Select(candidate => new AuthorDuplicateRow(candidate,
                localize(candidate.Reason == AuthorDuplicateReason.NameFormatting ? "AuthorDuplicatesFormatting" : "AuthorDuplicatesInitials"))).ToArray();
            StatusMessage = found.Truncated ? localize("AuthorDuplicatesLimit") : Candidates.Count == 0
                ? localize("AuthorDuplicatesNone") : string.Format(CultureInfo.CurrentCulture, localize("AuthorDuplicatesCount"), Candidates.Count, TotalAuthors);
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!disposed && current == version) { StatusMessage = localize("AuthorDuplicatesLoadFailed"); IsError = true; Candidates = []; } }
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

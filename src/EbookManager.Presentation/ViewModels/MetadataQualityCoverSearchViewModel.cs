using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Application.Metadata;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class MetadataQualityCoverSearchViewModel : ObservableObject
{
    private readonly BookCoverSearchQuery query;
    private readonly IBookCoverSearchService searchService;
    private readonly Func<string, string> localize;
    private bool isLoaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUseCover))]
    private BookCoverCandidate? selectedCandidate;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? statusMessage;

    public MetadataQualityCoverSearchViewModel(
        BookCoverSearchQuery query,
        IBookCoverSearchService searchService,
        Func<string, string> localize)
    {
        this.query = query;
        this.searchService = searchService;
        this.localize = localize;
        BookTitle = query.Title;
    }

    public string BookTitle { get; }
    public ObservableCollection<BookCoverCandidate> Candidates { get; } = [];
    public bool CanUseCover => SelectedCandidate is not null && !IsLoading;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (isLoaded)
        {
            return;
        }

        isLoaded = true;
        IsLoading = true;
        StatusMessage = localize("MetadataQualityCoverSearchLoading");
        OnPropertyChanged(nameof(CanUseCover));

        BookCoverSearchResult result;
        try
        {
            result = await searchService.SearchAsync(query, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            result = new(BookCoverSearchStatus.Failed, []);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(CanUseCover));
        }

        Candidates.Clear();
        foreach (var candidate in result.Candidates)
        {
            Candidates.Add(candidate);
        }

        SelectedCandidate = null;
        StatusMessage = result.Status switch
        {
            BookCoverSearchStatus.Succeeded when Candidates.Count > 0 => null,
            BookCoverSearchStatus.NoResults => localize("MetadataQualityCoverSearchNoResults"),
            _ => localize("MetadataQualityCoverSearchFailed")
        };
    }
}

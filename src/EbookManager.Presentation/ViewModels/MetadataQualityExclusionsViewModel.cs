using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Metadata;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class MetadataQualityExclusionsViewModel : ObservableObject
{
    private readonly IMetadataQualityExclusionRepository repository;
    private readonly Func<string, string> localize;
    private readonly AsyncRelayCommand restoreSelectedCommand;
    private readonly AsyncRelayCommand restoreAllCommand;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isRestoring;

    public MetadataQualityExclusionsViewModel(
        IMetadataQualityExclusionRepository repository,
        Func<string, string> localize)
    {
        this.repository = repository;
        this.localize = localize;
        restoreSelectedCommand = new AsyncRelayCommand(RestoreSelectedAsync, () => !IsRestoring && SelectedRows.Count > 0);
        restoreAllCommand = new AsyncRelayCommand(RestoreAllAsync, () => CanRestoreAll);
    }

    public ObservableCollection<MetadataQualityExclusionRowViewModel> Rows { get; } = [];
    public ObservableCollection<MetadataQualityExclusionRowViewModel> SelectedRows { get; } = [];
    public bool HasRows => Rows.Count > 0;
    public bool CanRestoreAll => HasRows && !IsRestoring;

    partial void OnIsRestoringChanged(bool value) => NotifyStateChanged();
    public int ExclusionCount => Rows.Count;
    public IAsyncRelayCommand RestoreSelectedCommand => restoreSelectedCommand;
    public IAsyncRelayCommand RestoreAllCommand => restoreAllCommand;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        Rows.Clear();
        foreach (var exclusion in await repository.ListMetadataQualityExclusionDetailsAsync(cancellationToken))
        {
            Rows.Add(new MetadataQualityExclusionRowViewModel(
                exclusion,
                LocalizeSignal(exclusion.Key.SignalKey)));
        }

        SelectedRows.Clear();
        NotifyStateChanged();
    }

    public void SetSelectedRows(IEnumerable<MetadataQualityExclusionRowViewModel> selectedRows)
    {
        SelectedRows.Clear();
        foreach (var row in selectedRows)
        {
            SelectedRows.Add(row);
        }

        NotifyStateChanged();
    }

    private Task RestoreSelectedAsync(CancellationToken cancellationToken) =>
        RestoreAsync(SelectedRows.Where(Rows.Contains).DistinctBy(row => row.Key).ToArray(),
            restoreAll: false, cancellationToken);

    private Task RestoreAllAsync(CancellationToken cancellationToken) =>
        RestoreAsync(Rows.ToArray(), restoreAll: true, cancellationToken);

    private async Task RestoreAsync(IReadOnlyList<MetadataQualityExclusionRowViewModel> rows,
        bool restoreAll, CancellationToken cancellationToken)
    {
        if (IsRestoring || rows.Count == 0)
        {
            return;
        }

        IsRestoring = true;
        StatusMessage = null;
        try
        {
            if (restoreAll)
            {
                await repository.ClearMetadataQualityExclusionsAsync(cancellationToken);
            }
            else
            {
                await repository.RemoveMetadataQualityExclusionsAsync(
                    rows.Select(row => row.Key).ToArray(), cancellationToken);
            }

            if (restoreAll)
            {
                Rows.Clear();
                SelectedRows.Clear();
            }
            else
            {
                // The live selection can change while storage is awaited.
                foreach (var row in rows)
                {
                    Rows.Remove(row);
                }
                foreach (var row in SelectedRows.Where(row => !Rows.Contains(row)).ToArray())
                {
                    SelectedRows.Remove(row);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            StatusMessage = localize("MetadataQualityExclusionsRestoreFailed");
        }
        finally
        {
            IsRestoring = false;
        }
    }

    private string LocalizeSignal(string signalKey)
    {
        var localizationKey = signalKey switch
        {
            MetadataQualitySignalKeys.MissingAuthor => "MetadataQualityMissingAuthor",
            MetadataQualitySignalKeys.UnknownLanguage => "MetadataQualityUnknownLanguage",
            MetadataQualitySignalKeys.MissingCover => "MetadataQualityMissingCover",
            MetadataQualitySignalKeys.SeriesNumberWithoutSeries => "MetadataQualitySeriesNumberWithoutSeries",
            MetadataQualitySignalKeys.PossibleTitleAuthorSwap => "MetadataQualityPossibleTitleAuthorSwap",
            MetadataQualitySignalKeys.MessyTags => "MetadataQualityMessyTags",
            _ => null
        };
        return localizationKey is null ? signalKey : localize(localizationKey);
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(CanRestoreAll));
        OnPropertyChanged(nameof(ExclusionCount));
        restoreSelectedCommand.NotifyCanExecuteChanged();
        restoreAllCommand.NotifyCanExecuteChanged();
    }
}

public sealed class MetadataQualityExclusionRowViewModel(
    MetadataQualityExclusion exclusion,
    string signal)
{
    public MetadataQualityExclusionKey Key { get; } = exclusion.Key;
    public string BookTitle { get; } = exclusion.BookTitle;
    public string BookAuthors { get; } = string.Join(", ", exclusion.BookAuthors);
    public string Signal { get; } = signal;
    public DateTimeOffset CreatedAt { get; } = exclusion.CreatedAt;
}

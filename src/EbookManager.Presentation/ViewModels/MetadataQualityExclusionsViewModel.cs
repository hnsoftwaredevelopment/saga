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

    public MetadataQualityExclusionsViewModel(
        IMetadataQualityExclusionRepository repository,
        Func<string, string> localize)
    {
        this.repository = repository;
        this.localize = localize;
        restoreSelectedCommand = new AsyncRelayCommand(RestoreSelectedAsync, () => SelectedRows.Count > 0);
        restoreAllCommand = new AsyncRelayCommand(RestoreAllAsync, () => Rows.Count > 0);
    }

    public ObservableCollection<MetadataQualityExclusionRowViewModel> Rows { get; } = [];
    public ObservableCollection<MetadataQualityExclusionRowViewModel> SelectedRows { get; } = [];
    public bool HasRows => Rows.Count > 0;
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

    private async Task RestoreSelectedAsync(CancellationToken cancellationToken)
    {
        var keys = SelectedRows.Select(row => row.Key).ToArray();
        if (keys.Length == 0)
        {
            return;
        }

        await repository.RemoveMetadataQualityExclusionsAsync(keys, cancellationToken);
        foreach (var row in SelectedRows.ToArray())
        {
            Rows.Remove(row);
        }

        SelectedRows.Clear();
        NotifyStateChanged();
    }

    private async Task RestoreAllAsync(CancellationToken cancellationToken)
    {
        if (Rows.Count == 0)
        {
            return;
        }

        await repository.ClearMetadataQualityExclusionsAsync(cancellationToken);
        Rows.Clear();
        SelectedRows.Clear();
        NotifyStateChanged();
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

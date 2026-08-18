using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class DuplicateExclusionsViewModel : ObservableObject
{
    private readonly IDuplicateExclusionRepository repository;
    private readonly AsyncRelayCommand restoreSelectedCommand;
    private readonly AsyncRelayCommand restoreAllCommand;

    public DuplicateExclusionsViewModel(IDuplicateExclusionRepository repository)
    {
        this.repository = repository;
        restoreSelectedCommand = new AsyncRelayCommand(RestoreSelectedAsync, () => SelectedRows.Count > 0);
        restoreAllCommand = new AsyncRelayCommand(RestoreAllAsync, () => Rows.Count > 0);
    }

    public ObservableCollection<DuplicateExclusionRowViewModel> Rows { get; } = [];
    public ObservableCollection<DuplicateExclusionRowViewModel> SelectedRows { get; } = [];
    public bool HasRows => Rows.Count > 0;
    public int ExclusionCount => Rows.Count;
    public IAsyncRelayCommand RestoreSelectedCommand => restoreSelectedCommand;
    public IAsyncRelayCommand RestoreAllCommand => restoreAllCommand;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        Rows.Clear();
        foreach (var exclusion in await repository.ListDuplicateExclusionDetailsAsync(cancellationToken))
        {
            Rows.Add(new DuplicateExclusionRowViewModel(exclusion));
        }

        SelectedRows.Clear();
        NotifyStateChanged();
    }

    public void SetSelectedRows(IEnumerable<DuplicateExclusionRowViewModel> selectedRows)
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
        var pairs = SelectedRows.Select(row => row.Pair).ToArray();
        if (pairs.Length == 0)
        {
            return;
        }

        await repository.RemoveDuplicateExclusionsAsync(pairs, cancellationToken);
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

        await repository.ClearDuplicateExclusionsAsync(cancellationToken);
        Rows.Clear();
        SelectedRows.Clear();
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(ExclusionCount));
        restoreSelectedCommand.NotifyCanExecuteChanged();
        restoreAllCommand.NotifyCanExecuteChanged();
    }
}

public sealed class DuplicateExclusionRowViewModel(DuplicateExclusion exclusion)
{
    public DuplicateExclusionPair Pair { get; } = exclusion.Pair;
    public string FirstBookTitle { get; } = exclusion.FirstBookTitle;
    public string FirstBookAuthors { get; } = FormatAuthors(exclusion.FirstBookAuthors);
    public string SecondBookTitle { get; } = exclusion.SecondBookTitle;
    public string SecondBookAuthors { get; } = FormatAuthors(exclusion.SecondBookAuthors);
    public DateTimeOffset CreatedAt { get; } = exclusion.CreatedAt;

    private static string FormatAuthors(IReadOnlyList<string> authors) =>
        authors.Count == 0
            ? string.Empty
            : string.Join(", ", authors);
}

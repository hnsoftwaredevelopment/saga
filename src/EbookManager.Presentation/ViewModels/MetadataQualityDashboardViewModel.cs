using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class MetadataQualityDashboardViewModel : ObservableObject
{
    private readonly Func<string, string> localize;
    private readonly IMetadataQualityExclusionRepository? repository;
    private readonly IMetadataQualityAuthorRepairService? authorRepairService;
    private readonly Func<MetadataQualityAuthorRepairViewModel, CancellationToken, Task<bool>>? showAuthorRepair;
    private readonly Action<Book>? bookRepaired;
    private readonly Dictionary<Guid, Book> books;
    private readonly HashSet<MetadataQualityExclusionKey> exclusions;
    private readonly AsyncRelayCommand markSelectedIssueCorrectCommand;
    private readonly AsyncRelayCommand repairMissingAuthorCommand;

    [ObservableProperty]
    private MetadataQualityIssueViewModel? selectedIssue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedBookId))]
    [NotifyPropertyChangedFor(nameof(CanOpenSelectedBook))]
    private MetadataQualityBookRowViewModel? selectedBook;

    [ObservableProperty]
    private string? statusMessage;

    public MetadataQualityDashboardViewModel(
        IReadOnlyList<Book> books,
        Func<string, string> localize,
        IReadOnlySet<MetadataQualityExclusionKey>? exclusions = null,
        IMetadataQualityExclusionRepository? repository = null,
        IMetadataQualityAuthorRepairService? authorRepairService = null,
        Func<MetadataQualityAuthorRepairViewModel, CancellationToken, Task<bool>>? showAuthorRepair = null,
        Action<Book>? bookRepaired = null)
    {
        this.localize = localize;
        this.repository = repository;
        this.authorRepairService = authorRepairService;
        this.showAuthorRepair = showAuthorRepair;
        this.bookRepaired = bookRepaired;
        this.books = books.ToDictionary(book => book.Id);
        this.exclusions = exclusions is null ? [] : [.. exclusions];
        markSelectedIssueCorrectCommand = new AsyncRelayCommand(
            MarkSelectedIssueCorrectAsync,
            CanMarkSelectedIssueCorrect);
        repairMissingAuthorCommand = new AsyncRelayCommand(
            RepairMissingAuthorAsync,
            CanRepairMissingAuthor);
        TotalBookCount = books.Count;
        Issues = new ObservableCollection<MetadataQualityIssueViewModel>(
            BuildIssues(
                books,
                localize,
                this.exclusions));
        SelectedIssue = Issues.FirstOrDefault(issue => issue.Count > 0) ?? Issues.FirstOrDefault();
    }

    public int TotalBookCount { get; }
    public ObservableCollection<MetadataQualityIssueViewModel> Issues { get; }
    public bool HasIssues => Issues.Any(issue => issue.Count > 0);
    public int TotalIssueCount => Issues.Sum(issue => issue.Count);
    public Guid? SelectedBookId => SelectedBook?.Id;
    public bool CanOpenSelectedBook => SelectedBook is not null;
    public IAsyncRelayCommand MarkSelectedIssueCorrectCommand => markSelectedIssueCorrectCommand;
    public IAsyncRelayCommand RepairMissingAuthorCommand => repairMissingAuthorCommand;

    partial void OnSelectedIssueChanged(MetadataQualityIssueViewModel? value)
    {
        SelectedBook = value?.Rows.FirstOrDefault();
        markSelectedIssueCorrectCommand.NotifyCanExecuteChanged();
        repairMissingAuthorCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedBookChanged(MetadataQualityBookRowViewModel? value)
    {
        markSelectedIssueCorrectCommand.NotifyCanExecuteChanged();
        repairMissingAuthorCommand.NotifyCanExecuteChanged();
    }

    private static IReadOnlyList<MetadataQualityIssueViewModel> BuildIssues(
        IReadOnlyList<Book> books,
        Func<string, string> localize,
        IReadOnlySet<MetadataQualityExclusionKey> exclusions) =>
        [
            CreateIssue(
                MetadataQualitySignalKeys.MissingAuthor,
                localize("MetadataQualityMissingAuthor"),
                localize("MetadataQualityMissingAuthorDescription"),
                books,
                exclusions),
            CreateIssue(
                MetadataQualitySignalKeys.UnknownLanguage,
                localize("MetadataQualityUnknownLanguage"),
                localize("MetadataQualityUnknownLanguageDescription"),
                books,
                exclusions),
            CreateIssue(
                MetadataQualitySignalKeys.MissingCover,
                localize("MetadataQualityMissingCover"),
                localize("MetadataQualityMissingCoverDescription"),
                books,
                exclusions),
            CreateIssue(
                MetadataQualitySignalKeys.SeriesNumberWithoutSeries,
                localize("MetadataQualitySeriesNumberWithoutSeries"),
                localize("MetadataQualitySeriesNumberWithoutSeriesDescription"),
                books,
                exclusions),
            CreateIssue(
                MetadataQualitySignalKeys.PossibleTitleAuthorSwap,
                localize("MetadataQualityPossibleTitleAuthorSwap"),
                localize("MetadataQualityPossibleTitleAuthorSwapDescription"),
                books,
                exclusions),
            CreateIssue(
                MetadataQualitySignalKeys.MessyTags,
                localize("MetadataQualityMessyTags"),
                localize("MetadataQualityMessyTagsDescription"),
                books,
                exclusions)
        ];

    private static MetadataQualityIssueViewModel CreateIssue(
        string signalKey,
        string title,
        string description,
        IEnumerable<Book> books,
        IReadOnlySet<MetadataQualityExclusionKey> exclusions)
    {
        var rows = books
            .Where(book => MetadataQualitySignalEvaluator.Applies(book, signalKey))
            .Where(book => !exclusions.Contains(new MetadataQualityExclusionKey(book.Id, signalKey)))
            .OrderBy(book => book.Metadata.Title, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(book => string.Join(", ", book.Metadata.Authors), StringComparer.CurrentCultureIgnoreCase)
            .Select(book => new MetadataQualityBookRowViewModel(book))
            .ToArray();
        return new MetadataQualityIssueViewModel(signalKey, title, description, rows);
    }

    private bool CanMarkSelectedIssueCorrect() =>
        repository is not null &&
        SelectedIssue is not null &&
        SelectedBook is not null &&
        SelectedIssue.Rows.Contains(SelectedBook);

    private async Task MarkSelectedIssueCorrectAsync()
    {
        var issue = SelectedIssue;
        var book = SelectedBook;
        if (repository is null || issue is null || book is null || !issue.Rows.Contains(book))
        {
            return;
        }

        var key = new MetadataQualityExclusionKey(book.Id, issue.SignalKey);
        try
        {
            await repository.AddMetadataQualityExclusionsAsync([key], CancellationToken.None);
        }
        catch (Exception)
        {
            StatusMessage = localize("MetadataQualityMarkCorrectFailed");
            return;
        }

        exclusions.Add(key);

        var removedIndex = issue.Rows.IndexOf(book);
        if (removedIndex < 0)
        {
            return;
        }

        issue.Rows.RemoveAt(removedIndex);
        SelectedBook = issue.Rows.Count == 0
            ? null
            : issue.Rows[Math.Min(removedIndex, issue.Rows.Count - 1)];
        StatusMessage = null;
        OnPropertyChanged(nameof(HasIssues));
        OnPropertyChanged(nameof(TotalIssueCount));
        markSelectedIssueCorrectCommand.NotifyCanExecuteChanged();
    }

    private bool CanRepairMissingAuthor() =>
        authorRepairService is not null &&
        showAuthorRepair is not null &&
        SelectedIssue?.SignalKey == MetadataQualitySignalKeys.MissingAuthor &&
        SelectedBook is not null &&
        SelectedIssue.Rows.Contains(SelectedBook);

    private async Task RepairMissingAuthorAsync(CancellationToken cancellationToken)
    {
        var selectedBook = SelectedBook;
        if (!CanRepairMissingAuthor() || selectedBook is null ||
            authorRepairService is null || showAuthorRepair is null)
        {
            return;
        }

        var repair = new MetadataQualityAuthorRepairViewModel(
            selectedBook.Title,
            books.Values.SelectMany(book => book.Metadata.Authors));
        if (!await showAuthorRepair(repair, cancellationToken) || repair.NormalizedAuthor is not { } author)
        {
            return;
        }

        MetadataQualityAuthorRepairItemResult? result;
        try
        {
            result = (await authorRepairService.RepairAsync([selectedBook.Id], author, cancellationToken))
                .Items.SingleOrDefault(item => item.BookId == selectedBook.Id);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            StatusMessage = localize("MetadataQualityAuthorRepairFailed");
            return;
        }

        if (result?.Book is { } repairedBook)
        {
            ReconcileBook(repairedBook);
        }
        else if (result?.Status == MetadataQualityAuthorRepairStatus.NotFound)
        {
            RemoveBook(selectedBook.Id);
        }

        StatusMessage = result?.Status == MetadataQualityAuthorRepairStatus.Succeeded
            ? null
            : localize(result?.Status == MetadataQualityAuthorRepairStatus.NotFound
                ? "MetadataQualityBookUnavailableMessage"
                : "MetadataQualityAuthorRepairFailed");
    }

    private void ReconcileBook(Book book)
    {
        books[book.Id] = book;
        bookRepaired?.Invoke(book);
        var selectedIssue = SelectedIssue;
        var selectedIndex = selectedIssue?.Rows.IndexOf(SelectedBook!) ?? -1;
        var applicableSignals = MetadataQualitySignalEvaluator.Evaluate(book);

        foreach (var issue in Issues)
        {
            var existing = issue.Rows.SingleOrDefault(row => row.Id == book.Id);
            if (existing is not null)
            {
                issue.Rows.Remove(existing);
            }

            var shouldShow = applicableSignals.Contains(issue.SignalKey) &&
                !exclusions.Contains(new MetadataQualityExclusionKey(book.Id, issue.SignalKey));
            if (shouldShow)
            {
                InsertSorted(issue.Rows, new MetadataQualityBookRowViewModel(book));
            }
        }

        RestoreSelectionAfterBookChange(selectedIssue, selectedIndex, book.Id);
        NotifyDashboardStateChanged();
    }

    private void RemoveBook(Guid bookId)
    {
        books.Remove(bookId);
        var selectedIssue = SelectedIssue;
        var selectedIndex = selectedIssue?.Rows.IndexOf(SelectedBook!) ?? -1;
        foreach (var issue in Issues)
        {
            var row = issue.Rows.SingleOrDefault(candidate => candidate.Id == bookId);
            if (row is not null)
            {
                issue.Rows.Remove(row);
            }
        }

        RestoreSelectionAfterBookChange(selectedIssue, selectedIndex, bookId);
        NotifyDashboardStateChanged();
    }

    private void RestoreSelectionAfterBookChange(
        MetadataQualityIssueViewModel? selectedIssue,
        int selectedIndex,
        Guid bookId)
    {
        if (selectedIssue is null)
        {
            SelectedBook = null;
            return;
        }

        SelectedBook = selectedIssue.Rows.SingleOrDefault(row => row.Id == bookId) ??
            (selectedIssue.Rows.Count == 0
                ? null
                : selectedIssue.Rows[Math.Clamp(selectedIndex, 0, selectedIssue.Rows.Count - 1)]);
    }

    private static void InsertSorted(
        ObservableCollection<MetadataQualityBookRowViewModel> rows,
        MetadataQualityBookRowViewModel row)
    {
        var index = 0;
        while (index < rows.Count && CompareRows(rows[index], row) <= 0)
        {
            index++;
        }

        rows.Insert(index, row);
    }

    private static int CompareRows(
        MetadataQualityBookRowViewModel left,
        MetadataQualityBookRowViewModel right)
    {
        var titleComparison = StringComparer.CurrentCultureIgnoreCase.Compare(left.Title, right.Title);
        return titleComparison != 0
            ? titleComparison
            : StringComparer.CurrentCultureIgnoreCase.Compare(left.Authors, right.Authors);
    }

    private void NotifyDashboardStateChanged()
    {
        OnPropertyChanged(nameof(HasIssues));
        OnPropertyChanged(nameof(TotalIssueCount));
        markSelectedIssueCorrectCommand.NotifyCanExecuteChanged();
        repairMissingAuthorCommand.NotifyCanExecuteChanged();
    }

}

public sealed class MetadataQualityIssueViewModel : ObservableObject
{
    public MetadataQualityIssueViewModel(
        string signalKey,
        string title,
        string description,
        IReadOnlyList<MetadataQualityBookRowViewModel> rows)
    {
        SignalKey = signalKey;
        Title = title;
        Description = description;
        Rows = new ObservableCollection<MetadataQualityBookRowViewModel>(rows);
        Rows.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Count));
    }

    public string SignalKey { get; }
    public string Title { get; }
    public string Description { get; }
    public int Count => Rows.Count;
    public ObservableCollection<MetadataQualityBookRowViewModel> Rows { get; }
}

public sealed class MetadataQualityBookRowViewModel(Book book)
{
    public Guid Id { get; } = book.Id;
    public string Title { get; } = book.Metadata.Title;
    public string Authors { get; } = string.Join(", ", book.Metadata.Authors);
    public string Series { get; } = book.Metadata.Series ?? string.Empty;
    public string Language { get; } = book.Metadata.Language ?? string.Empty;
    public string Tags { get; } = string.Join("; ", book.Metadata.Tags ?? []);
}

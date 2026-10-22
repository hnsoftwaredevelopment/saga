using System.Collections.ObjectModel;
using System.Globalization;
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
    private readonly IMetadataQualityLanguageRepairService? languageRepairService;
    private readonly Func<MetadataQualityLanguageRepairViewModel, CancellationToken, Task<bool>>? showLanguageRepair;
    private readonly IMetadataQualitySeriesRepairService? seriesRepairService;
    private readonly Func<MetadataQualitySeriesRepairViewModel, CancellationToken, Task<bool>>? showSeriesRepair;
    private readonly IMetadataQualityTitleAuthorRepairService? titleAuthorRepairService;
    private readonly Func<MetadataQualityTitleAuthorRepairViewModel, CancellationToken, Task<bool>>? showTitleAuthorRepair;
    private readonly IBookCoverSearchService? coverSearchService;
    private readonly Func<MetadataQualityCoverSearchViewModel, CancellationToken, Task<bool>>? showCoverSearch;
    private readonly IMetadataQualityCoverRepairService? coverRepairService;
    private readonly IMetadataQualityTagRepairService? tagRepairService;
    private readonly Func<MetadataQualityTagRepairViewModel, CancellationToken, Task<bool>>? showTagRepair;
    private readonly Func<int, CancellationToken, Task<bool>>? confirmMarkCorrect;
    private readonly Action<IReadOnlyList<Book>>? booksRepaired;
    private readonly Dictionary<Guid, Book> books;
    private readonly HashSet<MetadataQualityExclusionKey> exclusions;
    private readonly AsyncRelayCommand markSelectedIssueCorrectCommand;
    private readonly AsyncRelayCommand repairMissingAuthorCommand;
    private readonly AsyncRelayCommand repairUnknownLanguageCommand;
    private readonly AsyncRelayCommand repairMissingSeriesCommand;
    private readonly AsyncRelayCommand repairTitleAuthorCommand;
    private readonly AsyncRelayCommand searchCoverCommand;
    private readonly AsyncRelayCommand repairMessyTagsCommand;
    private IReadOnlyList<MetadataQualityBookRowViewModel> selectedBooks = [];

    [ObservableProperty]
    private MetadataQualityIssueViewModel? selectedIssue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedBookId))]
    [NotifyPropertyChangedFor(nameof(CanOpenSelectedBook))]
    private MetadataQualityBookRowViewModel? selectedBook;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isStatusMessageSuccess;

    public MetadataQualityDashboardViewModel(
        IReadOnlyList<Book> books,
        Func<string, string> localize,
        IReadOnlySet<MetadataQualityExclusionKey>? exclusions = null,
        IMetadataQualityExclusionRepository? repository = null,
        IMetadataQualityAuthorRepairService? authorRepairService = null,
        Func<MetadataQualityAuthorRepairViewModel, CancellationToken, Task<bool>>? showAuthorRepair = null,
        IMetadataQualityLanguageRepairService? languageRepairService = null,
        Func<MetadataQualityLanguageRepairViewModel, CancellationToken, Task<bool>>? showLanguageRepair = null,
        IMetadataQualitySeriesRepairService? seriesRepairService = null,
        Func<MetadataQualitySeriesRepairViewModel, CancellationToken, Task<bool>>? showSeriesRepair = null,
        Action<IReadOnlyList<Book>>? booksRepaired = null,
        IMetadataQualityTitleAuthorRepairService? titleAuthorRepairService = null,
        Func<MetadataQualityTitleAuthorRepairViewModel, CancellationToken, Task<bool>>? showTitleAuthorRepair = null,
        IBookCoverSearchService? coverSearchService = null,
        Func<MetadataQualityCoverSearchViewModel, CancellationToken, Task<bool>>? showCoverSearch = null,
        IMetadataQualityCoverRepairService? coverRepairService = null,
        IMetadataQualityTagRepairService? tagRepairService = null,
        Func<MetadataQualityTagRepairViewModel, CancellationToken, Task<bool>>? showTagRepair = null,
        Func<int, CancellationToken, Task<bool>>? confirmMarkCorrect = null)
    {
        this.localize = localize;
        this.repository = repository;
        this.authorRepairService = authorRepairService;
        this.showAuthorRepair = showAuthorRepair;
        this.languageRepairService = languageRepairService;
        this.showLanguageRepair = showLanguageRepair;
        this.seriesRepairService = seriesRepairService;
        this.showSeriesRepair = showSeriesRepair;
        this.titleAuthorRepairService = titleAuthorRepairService;
        this.showTitleAuthorRepair = showTitleAuthorRepair;
        this.coverSearchService = coverSearchService;
        this.showCoverSearch = showCoverSearch;
        this.coverRepairService = coverRepairService;
        this.tagRepairService = tagRepairService;
        this.showTagRepair = showTagRepair;
        this.confirmMarkCorrect = confirmMarkCorrect;
        this.booksRepaired = booksRepaired;
        this.books = books.ToDictionary(book => book.Id);
        this.exclusions = exclusions is null ? [] : [.. exclusions];
        markSelectedIssueCorrectCommand = new AsyncRelayCommand(
            MarkSelectedIssueCorrectAsync,
            CanMarkSelectedIssueCorrect);
        repairMissingAuthorCommand = new AsyncRelayCommand(
            RepairMissingAuthorAsync,
            CanRepairMissingAuthor);
        repairUnknownLanguageCommand = new AsyncRelayCommand(
            RepairUnknownLanguageAsync,
            CanRepairUnknownLanguage);
        repairMissingSeriesCommand = new AsyncRelayCommand(
            RepairMissingSeriesAsync,
            CanRepairMissingSeries);
        repairTitleAuthorCommand = new AsyncRelayCommand(
            RepairTitleAuthorAsync,
            CanRepairTitleAuthor);
        searchCoverCommand = new AsyncRelayCommand(
            SearchCoverAsync,
            CanSearchCover);
        repairMessyTagsCommand = new AsyncRelayCommand(
            RepairMessyTagsAsync,
            CanRepairMessyTags);
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
    public int SelectedBookCount => selectedBooks.Count;
    public Guid? SelectedBookId => SelectedBookCount == 1 ? SelectedBook?.Id : null;
    public bool CanOpenSelectedBook => SelectedBookCount == 1 && SelectedBook is not null;
    public IAsyncRelayCommand MarkSelectedIssueCorrectCommand => markSelectedIssueCorrectCommand;
    public IAsyncRelayCommand RepairMissingAuthorCommand => repairMissingAuthorCommand;
    public IAsyncRelayCommand RepairUnknownLanguageCommand => repairUnknownLanguageCommand;
    public IAsyncRelayCommand RepairMissingSeriesCommand => repairMissingSeriesCommand;
    public IAsyncRelayCommand RepairTitleAuthorCommand => repairTitleAuthorCommand;
    public IAsyncRelayCommand SearchCoverCommand => searchCoverCommand;
    public IAsyncRelayCommand RepairMessyTagsCommand => repairMessyTagsCommand;

    partial void OnSelectedIssueChanged(MetadataQualityIssueViewModel? value)
    {
        ClearRepairFeedback();
        SetSelectedBooks(value?.Rows.Take(1) ?? []);
    }

    partial void OnSelectedBookChanged(MetadataQualityBookRowViewModel? value)
    {
        if (value is null)
        {
            selectedBooks = [];
        }
        else if (!selectedBooks.Contains(value))
        {
            selectedBooks = [value];
        }

        NotifySelectionStateChanged();
    }

    public void SetSelectedBooks(IEnumerable<MetadataQualityBookRowViewModel> rows)
    {
        var issue = SelectedIssue;
        var validRows = issue is null
            ? []
            : rows
                .Where(issue.Rows.Contains)
                .DistinctBy(row => row.Id)
                .ToArray();
        selectedBooks = validRows;

        var primary = validRows.Contains(SelectedBook)
            ? SelectedBook
            : validRows.LastOrDefault();
        if (!ReferenceEquals(SelectedBook, primary))
        {
            SelectedBook = primary;
            return;
        }

        NotifySelectionStateChanged();
    }

    private void NotifySelectionStateChanged()
    {
        OnPropertyChanged(nameof(SelectedBookCount));
        OnPropertyChanged(nameof(SelectedBookId));
        OnPropertyChanged(nameof(CanOpenSelectedBook));
        markSelectedIssueCorrectCommand.NotifyCanExecuteChanged();
        repairMissingAuthorCommand.NotifyCanExecuteChanged();
        repairUnknownLanguageCommand.NotifyCanExecuteChanged();
        repairMissingSeriesCommand.NotifyCanExecuteChanged();
        repairTitleAuthorCommand.NotifyCanExecuteChanged();
        searchCoverCommand.NotifyCanExecuteChanged();
        repairMessyTagsCommand.NotifyCanExecuteChanged();
    }

    private static IReadOnlyList<MetadataQualityIssueViewModel> BuildIssues(
        IReadOnlyList<Book> books,
        Func<string, string> localize,
        IReadOnlySet<MetadataQualityExclusionKey> exclusions) =>
        BuildIssues(
            books.Select(book => (
                Book: book,
                Signals: MetadataQualitySignalEvaluator.Evaluate(book))).ToArray(),
            localize,
            exclusions);

    private static IReadOnlyList<MetadataQualityIssueViewModel> BuildIssues(
        IReadOnlyList<(Book Book, IReadOnlySet<string> Signals)> evaluatedBooks,
        Func<string, string> localize,
        IReadOnlySet<MetadataQualityExclusionKey> exclusions) =>
        [
            CreateIssue(
                MetadataQualitySignalKeys.MissingAuthor,
                localize("MetadataQualityMissingAuthor"),
                localize("MetadataQualityMissingAuthorDescription"),
                evaluatedBooks,
                exclusions),
            CreateIssue(
                MetadataQualitySignalKeys.UnknownLanguage,
                localize("MetadataQualityUnknownLanguage"),
                localize("MetadataQualityUnknownLanguageDescription"),
                evaluatedBooks,
                exclusions),
            CreateIssue(
                MetadataQualitySignalKeys.MissingCover,
                localize("MetadataQualityMissingCover"),
                localize("MetadataQualityMissingCoverDescription"),
                evaluatedBooks,
                exclusions),
            CreateIssue(
                MetadataQualitySignalKeys.SeriesNumberWithoutSeries,
                localize("MetadataQualitySeriesNumberWithoutSeries"),
                localize("MetadataQualitySeriesNumberWithoutSeriesDescription"),
                evaluatedBooks,
                exclusions),
            CreateIssue(
                MetadataQualitySignalKeys.PossibleTitleAuthorSwap,
                localize("MetadataQualityPossibleTitleAuthorSwap"),
                localize("MetadataQualityPossibleTitleAuthorSwapDescription"),
                evaluatedBooks,
                exclusions),
            CreateIssue(
                MetadataQualitySignalKeys.MessyTags,
                localize("MetadataQualityMessyTags"),
                localize("MetadataQualityMessyTagsDescription"),
                evaluatedBooks,
                exclusions)
        ];

    private static MetadataQualityIssueViewModel CreateIssue(
        string signalKey,
        string title,
        string description,
        IEnumerable<(Book Book, IReadOnlySet<string> Signals)> evaluatedBooks,
        IReadOnlySet<MetadataQualityExclusionKey> exclusions)
    {
        var rows = evaluatedBooks
            .Where(entry => entry.Signals.Contains(signalKey))
            .Select(entry => entry.Book)
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
        selectedBooks.Count > 0 &&
        selectedBooks.All(SelectedIssue.Rows.Contains) &&
        (selectedBooks.Count == 1 || confirmMarkCorrect is not null);

    private async Task MarkSelectedIssueCorrectAsync()
    {
        var issue = SelectedIssue;
        var rows = issue is null
            ? []
            : selectedBooks.Where(issue.Rows.Contains).DistinctBy(row => row.Id).ToArray();
        if (repository is null || issue is null || rows.Length == 0)
        {
            return;
        }

        if (rows.Length > 1 &&
            (confirmMarkCorrect is null || !await confirmMarkCorrect(rows.Length, CancellationToken.None)))
        {
            return;
        }

        ClearRepairFeedback();
        var keys = rows
            .Select(row => new MetadataQualityExclusionKey(row.Id, issue.SignalKey))
            .ToArray();
        try
        {
            await repository.AddMetadataQualityExclusionsAsync(keys, CancellationToken.None);
        }
        catch (Exception)
        {
            StatusMessage = localize("MetadataQualityMarkCorrectFailed");
            return;
        }

        foreach (var key in keys)
        {
            exclusions.Add(key);
        }

        var removedIndex = rows.Select(issue.Rows.IndexOf).Where(index => index >= 0).DefaultIfEmpty(0).Min();
        foreach (var row in rows)
        {
            issue.Rows.Remove(row);
        }

        SetSelectedBooks(issue.Rows.Count == 0
            ? []
            : [issue.Rows[Math.Min(removedIndex, issue.Rows.Count - 1)]]);
        StatusMessage = null;
        OnPropertyChanged(nameof(HasIssues));
        OnPropertyChanged(nameof(TotalIssueCount));
        NotifySelectionStateChanged();
    }

    private bool CanRepairMissingAuthor() =>
        authorRepairService is not null &&
        showAuthorRepair is not null &&
        selectedBooks.Count > 0 &&
        SelectedIssue?.SignalKey == MetadataQualitySignalKeys.MissingAuthor &&
        selectedBooks.All(SelectedIssue.Rows.Contains);

    private async Task RepairMissingAuthorAsync(CancellationToken cancellationToken)
    {
        var issue = SelectedIssue;
        var selectedRows = issue is null
            ? []
            : selectedBooks.Where(issue.Rows.Contains).DistinctBy(row => row.Id).ToArray();
        if (!CanRepairMissingAuthor() || issue is null || selectedRows.Length == 0 ||
            authorRepairService is null || showAuthorRepair is null)
        {
            return;
        }

        var repair = new MetadataQualityAuthorRepairViewModel(
            selectedRows.Select(row => row.Title).ToArray(),
            books.Values.SelectMany(book => book.Metadata.Authors),
            localize);
        if (!await showAuthorRepair(repair, cancellationToken) || repair.NormalizedAuthor is not { } author)
        {
            return;
        }

        ClearRepairFeedback();
        MetadataQualityAuthorRepairBatchResult batch;
        try
        {
            batch = await authorRepairService.RepairAsync(
                selectedRows.Select(row => row.Id).ToArray(),
                author,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            if (!ReferenceEquals(SelectedIssue, issue)) { return; }
            SetRepairFailureDetails(selectedRows);
            StatusMessage = localize("MetadataQualityAuthorRepairFailed");
            return;
        }

        ReconcileBatchRepair(
            issue,
            selectedRows,
            batch.Items.Select(ToBatchRepairItem).ToArray(),
            "MetadataQualityAuthorRepairBulkSucceeded",
            "MetadataQualityAuthorRepairBulkResult",
            "MetadataQualityAuthorRepairFailed",
            "MetadataQualityAuthorRepairWriteBackWarning",
            "MetadataQualityAuthorRepairNotNeeded");
    }

    partial void OnStatusMessageChanging(string? value) => IsStatusMessageSuccess = false;

    private bool CanRepairUnknownLanguage() =>
        languageRepairService is not null &&
        showLanguageRepair is not null &&
        selectedBooks.Count > 0 &&
        SelectedIssue?.SignalKey == MetadataQualitySignalKeys.UnknownLanguage &&
        selectedBooks.All(SelectedIssue.Rows.Contains);

    private async Task RepairUnknownLanguageAsync(CancellationToken cancellationToken)
    {
        var issue = SelectedIssue;
        var selectedRows = issue is null
            ? []
            : selectedBooks.Where(issue.Rows.Contains).DistinctBy(row => row.Id).ToArray();
        if (!CanRepairUnknownLanguage() || issue is null || selectedRows.Length == 0 ||
            languageRepairService is null || showLanguageRepair is null)
        {
            return;
        }

        var repair = new MetadataQualityLanguageRepairViewModel(
            selectedRows.Select(row => row.Title).ToArray(),
            localize);
        if (!await showLanguageRepair(repair, cancellationToken) ||
            repair.NormalizedLanguage is not { } language)
        {
            return;
        }

        ClearRepairFeedback();
        MetadataQualityLanguageRepairBatchResult batch;
        try
        {
            batch = await languageRepairService.RepairAsync(
                selectedRows.Select(row => row.Id).ToArray(),
                language,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            if (!ReferenceEquals(SelectedIssue, issue)) { return; }
            SetRepairFailureDetails(selectedRows);
            StatusMessage = localize("MetadataQualityLanguageRepairFailed");
            return;
        }

        ReconcileBatchRepair(
            issue,
            selectedRows,
            batch.Items.Select(ToBatchRepairItem).ToArray(),
            "MetadataQualityLanguageRepairBulkSucceeded",
            "MetadataQualityLanguageRepairBulkResult",
            "MetadataQualityLanguageRepairFailed",
            "MetadataQualityLanguageRepairWriteBackWarning",
            "MetadataQualityLanguageRepairNotNeeded");
    }

    private bool CanRepairMissingSeries() =>
        seriesRepairService is not null &&
        showSeriesRepair is not null &&
        selectedBooks.Count > 0 &&
        SelectedIssue?.SignalKey == MetadataQualitySignalKeys.SeriesNumberWithoutSeries &&
        selectedBooks.All(SelectedIssue.Rows.Contains);

    private async Task RepairMissingSeriesAsync(CancellationToken cancellationToken)
    {
        var issue = SelectedIssue;
        var selectedRows = issue is null
            ? []
            : selectedBooks.Where(issue.Rows.Contains).DistinctBy(row => row.Id).ToArray();
        if (!CanRepairMissingSeries() || issue is null || selectedRows.Length == 0 ||
            seriesRepairService is null || showSeriesRepair is null)
        {
            return;
        }

        var bookContexts = selectedRows
            .Select(row => books.TryGetValue(row.Id, out var book) &&
                book.Metadata.SeriesNumber is { } seriesNumber
                    ? (row.Title, SeriesNumber: seriesNumber)
                    : ((string Title, decimal SeriesNumber)?)null)
            .Where(context => context is not null)
            .Select(context => context!.Value)
            .ToArray();
        if (bookContexts.Length != selectedRows.Length)
        {
            if (!ReferenceEquals(SelectedIssue, issue)) { return; }
            SetRepairFailureDetails(selectedRows);
            StatusMessage = localize("MetadataQualitySeriesRepairFailed");
            return;
        }

        var repair = new MetadataQualitySeriesRepairViewModel(
            bookContexts,
            books.Values.Select(candidate => candidate.Metadata.Series),
            localize);
        if (!await showSeriesRepair(repair, cancellationToken) ||
            repair.NormalizedSeries is not { } series)
        {
            return;
        }

        ClearRepairFeedback();
        MetadataQualitySeriesRepairBatchResult batch;
        try
        {
            batch = await seriesRepairService.RepairAsync(
                selectedRows.Select(row => row.Id).ToArray(),
                series,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            if (!ReferenceEquals(SelectedIssue, issue)) { return; }
            SetRepairFailureDetails(selectedRows);
            StatusMessage = localize("MetadataQualitySeriesRepairFailed");
            return;
        }

        ReconcileBatchRepair(
            issue,
            selectedRows,
            batch.Items.Select(ToBatchRepairItem).ToArray(),
            "MetadataQualitySeriesRepairBulkSucceeded",
            "MetadataQualitySeriesRepairBulkResult",
            "MetadataQualitySeriesRepairFailed",
            "MetadataQualitySeriesRepairWriteBackWarning",
            "MetadataQualitySeriesRepairNotNeeded");
    }

    private bool CanRepairTitleAuthor() =>
        titleAuthorRepairService is not null &&
        showTitleAuthorRepair is not null &&
        selectedBooks.Count > 0 &&
        SelectedIssue?.SignalKey == MetadataQualitySignalKeys.PossibleTitleAuthorSwap &&
        selectedBooks.All(row => SelectedIssue.Rows.Contains(row) && books.ContainsKey(row.Id)) &&
        selectedBooks.Any(row => MetadataQualityTitleAuthorRepairRules.CanRepair(books[row.Id]));

    private async Task RepairTitleAuthorAsync(CancellationToken cancellationToken)
    {
        var issue = SelectedIssue;
        if (!CanRepairTitleAuthor() || issue is null ||
            titleAuthorRepairService is null || showTitleAuthorRepair is null)
        {
            return;
        }

        var selectedRows = selectedBooks.DistinctBy(row => row.Id).ToArray();
        var contexts = selectedRows.Select(row => books[row.Id]).ToArray();
        var singlePreview = contexts.Length == 1
            ? new MetadataQualityTitleAuthorRepairRowViewModel(contexts[0]) : null;
        var repair = singlePreview is not null
            ? new MetadataQualityTitleAuthorRepairViewModel(singlePreview.CurrentTitle, singlePreview.CurrentAuthor)
            : new MetadataQualityTitleAuthorRepairViewModel(contexts, localize);
        if (!await showTitleAuthorRepair(repair, cancellationToken) || !repair.CanConfirm)
        {
            return;
        }

        var proposals = singlePreview is not null
            ? [singlePreview]
            : repair.Previews.Where(row => row.CanRepair).ToArray();
        var chosenIds = proposals.Select(row => row.BookId).ToHashSet();
        var chosenRows = selectedRows.Where(row => chosenIds.Contains(row.Id)).ToArray();
        ClearRepairFeedback();
        var results = new List<MetadataQualityBatchRepairItem>();
        try
        {
            foreach (var proposal in proposals)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var result = await titleAuthorRepairService.RepairAsync(
                        proposal.BookId, cancellationToken, proposal.Snapshot);
                    results.Add(new MetadataQualityBatchRepairItem(result.BookId, result.Status switch
                    {
                        MetadataQualityTitleAuthorRepairStatus.Succeeded => MetadataQualityBatchRepairStatus.Succeeded,
                        MetadataQualityTitleAuthorRepairStatus.SavedWithWriteBackErrors => MetadataQualityBatchRepairStatus.SavedWithWriteBackErrors,
                        MetadataQualityTitleAuthorRepairStatus.NotApplicable => MetadataQualityBatchRepairStatus.NotApplicable,
                        MetadataQualityTitleAuthorRepairStatus.NotFound => MetadataQualityBatchRepairStatus.NotFound,
                        _ => MetadataQualityBatchRepairStatus.Failed
                    }, result.Book, result.FailureReason));
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception)
                {
                    results.Add(new MetadataQualityBatchRepairItem(proposal.BookId,
                        MetadataQualityBatchRepairStatus.Failed, null));
                }
            }
        }
        finally
        {
            ReconcileBatchRepair(issue, chosenRows, results,
                "MetadataQualityTitleAuthorRepairBulkSucceeded", "MetadataQualityTitleAuthorRepairBulkResult",
                "MetadataQualityTitleAuthorRepairFailed", "MetadataQualityTitleAuthorRepairWriteBackWarning",
                "MetadataQualityTitleAuthorRepairNotNeeded", isBulk: repair.IsBulk);
        }
    }

    private bool CanSearchCover() =>
        coverSearchService is not null &&
        showCoverSearch is not null &&
        coverRepairService is not null &&
        SelectedBookCount == 1 &&
        SelectedIssue?.SignalKey == MetadataQualitySignalKeys.MissingCover &&
        SelectedBook is not null &&
        SelectedIssue.Rows.Contains(SelectedBook) &&
        books.ContainsKey(SelectedBook.Id);

    private async Task SearchCoverAsync(CancellationToken cancellationToken)
    {
        var selectedBook = SelectedBook;
        if (!CanSearchCover() || selectedBook is null ||
            coverSearchService is null || showCoverSearch is null || coverRepairService is null ||
            !books.TryGetValue(selectedBook.Id, out var book))
        {
            return;
        }

        var search = new MetadataQualityCoverSearchViewModel(
            new BookCoverSearchQuery(
                book.Metadata.Title,
                book.Metadata.Authors,
                book.Metadata.Isbn),
            coverSearchService,
            localize);
        if (!await showCoverSearch(search, cancellationToken) ||
            search.SelectedCandidate is not { } candidate)
        {
            return;
        }

        ClearRepairFeedback();
        BookCoverDownloadResult downloadResult;
        try
        {
            downloadResult = await coverSearchService.DownloadAsync(
                candidate,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            if (SelectedIssue?.SignalKey != MetadataQualitySignalKeys.MissingCover) { return; }
            SetRepairFailureDetails([selectedBook], "MetadataQualityRepairReasonDownload");
            StatusMessage = localize("MetadataQualityCoverDownloadFailed");
            return;
        }

        if (downloadResult.Status != BookCoverDownloadStatus.Succeeded ||
            downloadResult.Bytes is not { Length: > 0 } coverBytes)
        {
            if (SelectedIssue?.SignalKey != MetadataQualitySignalKeys.MissingCover) { return; }
            SetRepairFailureDetails([selectedBook], "MetadataQualityRepairReasonDownload");
            StatusMessage = localize("MetadataQualityCoverDownloadFailed");
            return;
        }

        MetadataQualityCoverRepairResult repairResult;
        try
        {
            repairResult = await coverRepairService.RepairAsync(
                selectedBook.Id,
                coverBytes,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            if (SelectedIssue?.SignalKey != MetadataQualitySignalKeys.MissingCover) { return; }
            SetRepairFailureDetails([selectedBook]);
            StatusMessage = localize("MetadataQualityCoverRepairFailed");
            return;
        }

        if (repairResult.Book is { } repairedBook)
        {
            ReconcileBook(repairedBook);
        }
        else if (repairResult.Status == MetadataQualityCoverRepairStatus.NotFound)
        {
            RemoveBook(selectedBook.Id);
        }

        if (SelectedIssue?.SignalKey != MetadataQualitySignalKeys.MissingCover) { return; }
        if (repairResult.Status != MetadataQualityCoverRepairStatus.Succeeded)
        {
            AddRepairDetail(selectedBook, repairResult.Status switch
            {
                MetadataQualityCoverRepairStatus.SavedWithWriteBackErrors => MetadataQualityBatchRepairStatus.SavedWithWriteBackErrors,
                MetadataQualityCoverRepairStatus.NotFound => MetadataQualityBatchRepairStatus.NotFound,
                MetadataQualityCoverRepairStatus.NotApplicable => MetadataQualityBatchRepairStatus.NotApplicable,
                _ => MetadataQualityBatchRepairStatus.Failed
            }, repairResult.FailureReason);
        }

        StatusMessage = repairResult.Status switch
        {
            MetadataQualityCoverRepairStatus.Succeeded => null,
            MetadataQualityCoverRepairStatus.SavedWithWriteBackErrors =>
                localize("MetadataQualityCoverRepairWriteBackWarning"),
            MetadataQualityCoverRepairStatus.NotApplicable =>
                localize("MetadataQualityCoverRepairNotNeeded"),
            MetadataQualityCoverRepairStatus.NotFound =>
                localize("MetadataQualityBookUnavailableMessage"),
            _ => localize("MetadataQualityCoverRepairFailed")
        };
    }

    private bool CanRepairMessyTags() =>
        tagRepairService is not null &&
        showTagRepair is not null &&
        selectedBooks.Count > 0 &&
        SelectedIssue?.SignalKey == MetadataQualitySignalKeys.MessyTags &&
        selectedBooks.All(row => SelectedIssue.Rows.Contains(row) && books.ContainsKey(row.Id));

    private async Task RepairMessyTagsAsync(CancellationToken cancellationToken)
    {
        var issue = SelectedIssue;
        if (!CanRepairMessyTags() || issue is null ||
            tagRepairService is null || showTagRepair is null)
        {
            return;
        }

        var selectedRows = selectedBooks.DistinctBy(row => row.Id).ToArray();
        var contexts = selectedRows.Select(row => books[row.Id]).ToArray();
        var repair = contexts.Length == 1
            ? new MetadataQualityTagRepairViewModel(contexts[0].Metadata.Title, contexts[0].Metadata.Tags)
            : new MetadataQualityTagRepairViewModel(
                contexts.Select(book => (book.Id, book.Metadata.Title, book.Metadata.Tags)).ToArray(), localize);
        if (!await showTagRepair(repair, cancellationToken) || !repair.CanSave)
        {
            return;
        }

        var proposals = repair.IsBulk
            ? repair.Previews
            : [new MetadataQualityTagRepairPreview(contexts[0].Id, contexts[0].Metadata.Title,
                contexts[0].Metadata.Tags?.ToArray() ?? [], repair.NormalizedTags)];
        ClearRepairFeedback();
        var results = new List<MetadataQualityBatchRepairItem>();
        try
        {
            foreach (var proposal in proposals)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var result = await tagRepairService.RepairAsync(
                        proposal.BookId, proposal.Tags, cancellationToken, proposal.OriginalTags);
                    results.Add(new MetadataQualityBatchRepairItem(result.BookId, result.Status switch
                    {
                        MetadataQualityTagRepairStatus.Succeeded => MetadataQualityBatchRepairStatus.Succeeded,
                        MetadataQualityTagRepairStatus.SavedWithWriteBackErrors => MetadataQualityBatchRepairStatus.SavedWithWriteBackErrors,
                        MetadataQualityTagRepairStatus.NotApplicable => MetadataQualityBatchRepairStatus.NotApplicable,
                        MetadataQualityTagRepairStatus.NotFound => MetadataQualityBatchRepairStatus.NotFound,
                        _ => MetadataQualityBatchRepairStatus.Failed
                    }, result.Book, result.FailureReason));
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception)
                {
                    results.Add(new MetadataQualityBatchRepairItem(proposal.BookId, MetadataQualityBatchRepairStatus.Failed, null));
                }
            }
        }
        finally
        {
            ReconcileBatchRepair(issue, selectedRows, results,
                "MetadataQualityTagRepairBulkSucceeded", "MetadataQualityTagRepairBulkResult",
                "MetadataQualityTagRepairFailed", "MetadataQualityTagRepairWriteBackWarning",
                "MetadataQualityTagRepairNotNeeded");
        }
    }

    private void ReconcileBook(Book book, bool notifyLibrary = true)
    {
        books[book.Id] = book;
        if (notifyLibrary)
        {
            booksRepaired?.Invoke([book]);
        }
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
            SetSelectedBooks([]);
            return;
        }

        var selectedBook = selectedIssue.Rows.SingleOrDefault(row => row.Id == bookId) ??
            (selectedIssue.Rows.Count == 0
                ? null
                : selectedIssue.Rows[Math.Clamp(selectedIndex, 0, selectedIssue.Rows.Count - 1)]);
        SetSelectedBooks(selectedBook is null ? [] : [selectedBook]);
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
        NotifySelectionStateChanged();
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

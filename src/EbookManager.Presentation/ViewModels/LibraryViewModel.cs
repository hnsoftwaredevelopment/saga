using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EbookManager.Application.Books;
using EbookManager.Application.Importing;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.CustomMetadata;
using EbookManager.Domain.Importing;
using EbookManager.Domain.Settings;
using EbookManager.Libraries;
using EbookManager.Presentation.Abstractions;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class LibraryViewModel : ObservableObject
{
    private const int FilterSearchMinimumItemCount = 8;

    private const int LibraryLoadPageSize = 500;
    private const string MissingActiveLibraryMessage =
        "The active library folder no longer exists. Create or open a library to continue.";

    private readonly IBookRepository bookRepository;
    private readonly BookSearchService searchService;
    private readonly DuplicateCandidateService duplicateCandidateService;
    private readonly DuplicateMergeService duplicateMergeService;
    private readonly IUserInteractionService userInteraction;
    private readonly BookService? bookService;
    private readonly ImportService? importService;
    private readonly IImportAgent? importAgent;
    private readonly IImportRepository? importRepository;
    private readonly ICustomMetadataRepository? customMetadataRepository;
    private readonly IDuplicateExclusionRepository? duplicateExclusionRepository;
    private readonly LibraryService? libraryService;
    private readonly CurrentLibrary? currentLibrary;
    private readonly ILibraryDatabaseInitializer? databaseInitializer;
    private readonly DirectoryScanner? directoryScanner;
    private readonly IAppSettingsStore? settingsStore;
    private readonly ILibraryPerformanceReporter? performanceReporter;
    private readonly Func<string, string> localize;
    private readonly SemaphoreSlim settingsSaveLock = new(1, 1);
    private IReadOnlyList<Book> books = [];
    private Task pendingGroupingSettingsSave = Task.CompletedTask;
    private Task pendingSortSettingsSave = Task.CompletedTask;
    private long groupingSettingsSaveVersion;
    private bool isApplyingViewSortOption;
    private bool isApplyingViewDefinition;
    private bool isSuppressingFilterRefresh;
    private bool hasAppliedDefaultView;
    private int selectionVersion;
    private int bookRevealSequence;
    private AuthorSortStrategy authorSortStrategy = AuthorSortStrategy.DisplayName;
    private readonly Dictionary<string, List<LibraryGroupOption>> viewGroupings =
        BuiltInViewKeys().ToDictionary(key => key, _ => new List<LibraryGroupOption>(), StringComparer.Ordinal);
    private readonly Dictionary<string, LibrarySortOption> viewSortOptions =
        BuiltInViewKeys().ToDictionary(key => key, _ => LibrarySortOption.None, StringComparer.Ordinal);
    private IReadOnlyList<CustomMetadataFieldDefinition> customMetadataFieldDefinitions = [];
    private IReadOnlyDictionary<Guid, CustomMetadataFieldDefinition> customMetadataFieldDefinitionMap =
        new Dictionary<Guid, CustomMetadataFieldDefinition>();
    private Dictionary<Guid, IReadOnlyDictionary<Guid, string>> customMetadataValuesByBookId = [];
    private readonly Dictionary<string, List<LibraryColumnKey>> viewColumns =
        new()
        {
            [nameof(LibraryView.Bookshelf)] = [],
            [nameof(LibraryView.Detailed)] = DefaultDetailedColumns().Select(LibraryColumnKey.FromStandard).ToList(),
            [nameof(LibraryView.List)] = DefaultListColumns().Select(LibraryColumnKey.FromStandard).ToList()
        };
    private readonly Dictionary<string, Dictionary<LibraryColumnKey, double>> viewColumnWidths =
        new()
        {
            [nameof(LibraryView.Bookshelf)] = [],
            [nameof(LibraryView.Detailed)] = [],
            [nameof(LibraryView.List)] = []
        };

    public LibraryViewModel(
        IBookRepository bookRepository,
        BookSearchService searchService,
        BookDetailsViewModel details,
        IUserInteractionService userInteraction,
        DuplicateCandidateService? duplicateCandidateService = null,
        DuplicateMergeService? duplicateMergeService = null,
        BookService? bookService = null,
        ImportService? importService = null,
        IImportAgent? importAgent = null,
        IImportRepository? importRepository = null,
        ICustomMetadataRepository? customMetadataRepository = null,
        IDuplicateExclusionRepository? duplicateExclusionRepository = null,
        LibraryService? libraryService = null,
        CurrentLibrary? currentLibrary = null,
        ILibraryDatabaseInitializer? databaseInitializer = null,
        DirectoryScanner? directoryScanner = null,
        IAppSettingsStore? settingsStore = null,
        ILibraryPerformanceReporter? performanceReporter = null,
        Func<string, string>? localize = null)
    {
        this.bookRepository = bookRepository;
        this.searchService = searchService;
        this.duplicateCandidateService = duplicateCandidateService ?? new DuplicateCandidateService();
        this.duplicateMergeService = duplicateMergeService ?? new DuplicateMergeService(bookRepository);
        Details = details;
        this.userInteraction = userInteraction;
        this.bookService = bookService;
        this.importService = importService;
        this.importAgent = importAgent;
        this.importRepository = importRepository;
        this.customMetadataRepository = customMetadataRepository;
        this.duplicateExclusionRepository = duplicateExclusionRepository;
        this.libraryService = libraryService;
        this.currentLibrary = currentLibrary;
        this.databaseInitializer = databaseInitializer;
        this.directoryScanner = directoryScanner;
        this.settingsStore = settingsStore;
        this.performanceReporter = performanceReporter;
        this.localize = localize ?? DefaultGroupText;
        currentLibraryName = currentLibrary?.Current?.Name;
        currentLibraryPath = currentLibrary?.Current?.DirectoryPath;

        details.BookSaved += OnDetailsBookSaved;
        details.BookDeleted += OnDetailsBookDeleted;
        if (importAgent is not null)
        {
            importAgent.Completed += OnImportAgentCompleted;
        }
    }

    public BulkObservableCollection<BookRowViewModel> VisibleBooks { get; } = [];
    public ObservableCollection<BookRowViewModel> SelectedBooks { get; } = [];
    public ObservableCollection<FacetFilterViewModel> AuthorFilters { get; } = [];
    public ObservableCollection<FacetFilterViewModel> CategoryFilters { get; } = [];
    public ObservableCollection<FacetFilterViewModel> SeriesFilters { get; } = [];
    public ObservableCollection<FacetFilterViewModel> StatusFilters { get; } = [];
    public ObservableCollection<FacetFilterViewModel> EReaderFilters { get; } = [];
    public ObservableCollection<FacetFilterViewModel> LanguageFilters { get; } = [];
    public ObservableCollection<FacetFilterViewModel> FormatFilters { get; } = [];
    public ObservableCollection<CustomMetadataFilterGroupViewModel> CustomMetadataFilterGroups { get; } = [];
    public BulkObservableCollection<LibraryGroupNodeViewModel> GroupedLibraryNodes { get; } = [];
    public ObservableCollection<LibraryGroupOption> ActiveGroupOptions { get; } = [];
    public ObservableCollection<LibraryColumnKey> ActiveColumnOptions { get; } = [];
    public ObservableCollection<LibraryColumnChoiceViewModel> ColumnChoices { get; } = [];
    public ObservableCollection<LibraryViewDefinitionViewModel> ViewDefinitions { get; } = [];
    public LibraryViewDefinitionViewModel? SelectedViewDefinition =>
        ViewDefinitions.FirstOrDefault(definition =>
            definition.Id.Equals(SelectedViewDefinitionId, StringComparison.OrdinalIgnoreCase));
    public IReadOnlyList<LibraryGroupOption> AvailableGroupOptions { get; } =
    [
        LibraryGroupOption.Author,
        LibraryGroupOption.Series,
        LibraryGroupOption.Tag,
        LibraryGroupOption.Language,
        LibraryGroupOption.Status,
        LibraryGroupOption.Format
    ];
    public IReadOnlyList<LibraryColumnKey> AvailableColumnOptions { get; } =
        DefaultDetailedColumns().Select(LibraryColumnKey.FromStandard).ToArray();

    public BookDetailsViewModel Details { get; }

    public ImportJobViewModel? ImportJob => importAgent?.Job;

    public bool HasColumnChoices => ColumnChoices.Count > 0;

    public bool HasCustomMetadataFilterGroups => CustomMetadataFilterGroups.Count > 0;

    public int VisibleAuthorFilterCount => CountVisibleFilters(AuthorFilters);
    public int VisibleCategoryFilterCount => CountVisibleFilters(CategoryFilters);
    public int VisibleSeriesFilterCount => CountVisibleFilters(SeriesFilters);
    public int VisibleLanguageFilterCount => CountVisibleFilters(LanguageFilters);
    public int VisibleFormatFilterCount => CountVisibleFilters(FormatFilters);
    public string AuthorFilterSearchSummary => FormatFilterSearchCountSummary(VisibleAuthorFilterCount, AuthorFilters.Count);
    public string CategoryFilterSearchSummary => FormatFilterSearchCountSummary(VisibleCategoryFilterCount, CategoryFilters.Count);
    public string SeriesFilterSearchSummary => FormatFilterSearchCountSummary(VisibleSeriesFilterCount, SeriesFilters.Count);
    public string LanguageFilterSearchSummary => FormatFilterSearchCountSummary(VisibleLanguageFilterCount, LanguageFilters.Count);
    public string FormatFilterSearchSummary => FormatFilterSearchCountSummary(VisibleFormatFilterCount, FormatFilters.Count);
    public bool HasAuthorFilterSearch => ShouldShowFilterSearch(AuthorFilters.Count, AuthorFilterSearchText);
    public bool HasCategoryFilterSearch => ShouldShowFilterSearch(CategoryFilters.Count, CategoryFilterSearchText);
    public bool HasSeriesFilterSearch => ShouldShowFilterSearch(SeriesFilters.Count, SeriesFilterSearchText);
    public bool HasLanguageFilterSearch => ShouldShowFilterSearch(LanguageFilters.Count, LanguageFilterSearchText);
    public bool HasFormatFilterSearch => ShouldShowFilterSearch(FormatFilters.Count, FormatFilterSearchText);

    public string ApplicationVersion =>
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ??
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ??
        "-";

    public bool CanManageSelectedViewDefinition =>
        ViewDefinitions.FirstOrDefault(definition =>
            definition.Id.Equals(SelectedViewDefinitionId, StringComparison.OrdinalIgnoreCase)) is { IsBuiltIn: false };

    public int SelectedBookCount => SelectedBooks.Count;

    public bool CanMultiEditSelectedBooks => SelectedBooks.Count > 0;

    public string MetadataMultiEditMenuHeader =>
        $"{localize("MetadataMultiEditTitle")} ({SelectedBookCount})";

    public IReadOnlyList<LibraryColumnKey> ActiveColumnOptionsSnapshot => ActiveColumnOptions.ToArray();

    public LibraryColumnLayoutSnapshot ActiveColumnLayoutSnapshot =>
        new(ActiveColumnOptions.ToArray(), GetColumnWidths(SelectedLayoutKey), customMetadataFieldDefinitionMap);

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string authorFilterSearchText = string.Empty;

    [ObservableProperty]
    private string categoryFilterSearchText = string.Empty;

    [ObservableProperty]
    private string seriesFilterSearchText = string.Empty;

    [ObservableProperty]
    private string languageFilterSearchText = string.Empty;

    [ObservableProperty]
    private string formatFilterSearchText = string.Empty;

    [ObservableProperty]
    private LibraryView selectedView = LibraryView.Detailed;

    [ObservableProperty]
    private string selectedViewDefinitionId = nameof(LibraryView.Detailed);

    [ObservableProperty]
    private string activeViewLayoutKey = nameof(LibraryView.Detailed);

    [ObservableProperty]
    private LibrarySortOption selectedSortOption = LibrarySortOption.None;

    [ObservableProperty]
    private LibraryGroupOption selectedGroupOptionToAdd = LibraryGroupOption.Author;

    [ObservableProperty]
    private BookRowViewModel? selectedBook;

    [ObservableProperty]
    private LibraryBookRevealRequest? bookRevealRequest;

    [ObservableProperty]
    private ImportResultViewModel? lastImportResult;

    [ObservableProperty]
    private string? currentLibraryName;

    [ObservableProperty]
    private string? currentLibraryPath;

    [ObservableProperty]
    private string emptyStateMessage = "Create or open a library to get started.";

    [ObservableProperty]
    private bool isLoadingLibrary;

    [ObservableProperty]
    private int loadingLibraryTotalCount;

    [ObservableProperty]
    private int loadedLibraryCount;

    [ObservableProperty]
    private bool isCleaningMetadata;

    [ObservableProperty]
    private string metadataCleanupStatusText = "Updating metadata...";

    public double LoadingLibraryProgressValue =>
        LoadingLibraryTotalCount <= 0
            ? 0
            : Math.Min(100, LoadedLibraryCount * 100.0 / LoadingLibraryTotalCount);

    public bool IsLoadingLibraryProgressIndeterminate => LoadingLibraryTotalCount <= 0;

    public string LoadingLibraryProgressText =>
        LoadingLibraryTotalCount <= 0
            ? string.Empty
            : $"{LoadedLibraryCount} / {Math.Max(LoadingLibraryTotalCount, LoadedLibraryCount)}";

    public bool HasActiveLibrary => CurrentLibraryPath is not null;

    public bool HasActiveImport => importAgent?.IsActive == true;

    public int VisibleBookCount => VisibleBooks.Select(row => row.Id).Distinct().Count();

    public bool IsBookshelfGrouped => ActiveGroupOptions.Count > 0;

    public bool IsLibraryGrouped => IsBookshelfGrouped;

    public IEnumerable<BookRowViewModel>? BookshelfVisibleBooksSource =>
        SelectedView == LibraryView.Bookshelf && !IsBookshelfGrouped ? VisibleBooks : null;

    public IEnumerable<LibraryGroupNodeViewModel>? BookshelfGroupedLibraryNodesSource =>
        SelectedView == LibraryView.Bookshelf && IsBookshelfGrouped ? GroupedLibraryNodes : null;

    public IEnumerable<BookRowViewModel>? DetailedVisibleBooksSource =>
        SelectedView == LibraryView.Detailed && !IsLibraryGrouped ? VisibleBooks : null;

    public IEnumerable<LibraryGroupNodeViewModel>? DetailedGroupedLibraryNodesSource =>
        SelectedView == LibraryView.Detailed && IsLibraryGrouped ? GroupedLibraryNodes : null;

    public IEnumerable<BookRowViewModel>? ListVisibleBooksSource =>
        SelectedView == LibraryView.List && !IsLibraryGrouped ? VisibleBooks : null;

    public IEnumerable<LibraryGroupNodeViewModel>? ListGroupedLibraryNodesSource =>
        SelectedView == LibraryView.List && IsLibraryGrouped ? GroupedLibraryNodes : null;

    public IAsyncRelayCommand RefreshCommand => refreshCommand ??= new AsyncRelayCommand(RefreshAsync);
    public IAsyncRelayCommand AddBooksCommand => addBooksCommand ??= new AsyncRelayCommand(AddBooksAsync);
    public IAsyncRelayCommand ScanFolderCommand => scanFolderCommand ??= new AsyncRelayCommand(ScanFolderAsync);
    public IAsyncRelayCommand CreateLibraryCommand => createLibraryCommand ??= new AsyncRelayCommand(CreateLibraryAsync);
    public IAsyncRelayCommand OpenLibraryCommand => openLibraryCommand ??= new AsyncRelayCommand(OpenLibraryAsync);
    public IRelayCommand CancelImportCommand => cancelImportCommand ??= new RelayCommand(() => importAgent?.CancelActiveJob());
    public IAsyncRelayCommand ShowImportDetailsCommand => showImportDetailsCommand ??= new AsyncRelayCommand(ShowImportDetailsAsync);
    public IAsyncRelayCommand ShowImportHistoryCommand => showImportHistoryCommand ??= new AsyncRelayCommand(ShowImportHistoryAsync);
    public IAsyncRelayCommand ShowDuplicateCandidatesCommand => showDuplicateCandidatesCommand ??= new AsyncRelayCommand(ShowDuplicateCandidatesAsync);

    public IAsyncRelayCommand ShowDuplicateExclusionsCommand => showDuplicateExclusionsCommand ??= new AsyncRelayCommand(
        ShowDuplicateExclusionsAsync,
        () => duplicateExclusionRepository is not null && HasActiveLibrary);
    public IAsyncRelayCommand ShowMetadataQualityDashboardCommand =>
        showMetadataQualityDashboardCommand ??= new AsyncRelayCommand(ShowMetadataQualityDashboardAsync, () => HasActiveLibrary);
    public IRelayCommand CloseImportJobCommand => closeImportJobCommand ??= new RelayCommand(() => importAgent?.Job.Close());
    public IAsyncRelayCommand AddGroupingCommand => addGroupingCommand ??= new AsyncRelayCommand(AddGroupingAsync, CanAddGrouping);
    public IAsyncRelayCommand<LibraryGroupOption> RemoveGroupingCommand =>
        removeGroupingCommand ??= new AsyncRelayCommand<LibraryGroupOption>(RemoveGroupingAsync);
    public IAsyncRelayCommand<FacetFilterViewModel> RenameAuthorFilterCommand =>
        renameAuthorFilterCommand ??= new AsyncRelayCommand<FacetFilterViewModel>(filter => RenameFilterValueAsync(filter, MetadataFilterKind.Author));
    public IAsyncRelayCommand<FacetFilterViewModel> RemoveAuthorFilterCommand =>
        removeAuthorFilterCommand ??= new AsyncRelayCommand<FacetFilterViewModel>(filter => RemoveFilterValueAsync(filter, MetadataFilterKind.Author));
    public IAsyncRelayCommand<FacetFilterViewModel> RenameSeriesFilterCommand =>
        renameSeriesFilterCommand ??= new AsyncRelayCommand<FacetFilterViewModel>(filter => RenameFilterValueAsync(filter, MetadataFilterKind.Series));
    public IAsyncRelayCommand<FacetFilterViewModel> RemoveSeriesFilterCommand =>
        removeSeriesFilterCommand ??= new AsyncRelayCommand<FacetFilterViewModel>(filter => RemoveFilterValueAsync(filter, MetadataFilterKind.Series));
    public IAsyncRelayCommand<FacetFilterViewModel> RenameTagFilterCommand =>
        renameTagFilterCommand ??= new AsyncRelayCommand<FacetFilterViewModel>(filter => RenameFilterValueAsync(filter, MetadataFilterKind.Tag));
    public IAsyncRelayCommand<FacetFilterViewModel> RemoveTagFilterCommand =>
        removeTagFilterCommand ??= new AsyncRelayCommand<FacetFilterViewModel>(filter => RemoveFilterValueAsync(filter, MetadataFilterKind.Tag));
    public IAsyncRelayCommand<FacetFilterViewModel> RenameLanguageFilterCommand =>
        renameLanguageFilterCommand ??= new AsyncRelayCommand<FacetFilterViewModel>(filter => RenameFilterValueAsync(filter, MetadataFilterKind.Language));
    public IAsyncRelayCommand<FacetFilterViewModel> RemoveLanguageFilterCommand =>
        removeLanguageFilterCommand ??= new AsyncRelayCommand<FacetFilterViewModel>(filter => RemoveFilterValueAsync(filter, MetadataFilterKind.Language));
    public IAsyncRelayCommand<FacetFilterViewModel> RenameCustomMetadataFilterCommand =>
        renameCustomMetadataFilterCommand ??= new AsyncRelayCommand<FacetFilterViewModel>(filter => RenameCustomMetadataFilterValueAsync(filter));
    public IAsyncRelayCommand<FacetFilterViewModel> RemoveCustomMetadataFilterCommand =>
        removeCustomMetadataFilterCommand ??= new AsyncRelayCommand<FacetFilterViewModel>(filter => RemoveCustomMetadataFilterValueAsync(filter));
    public IAsyncRelayCommand NormalizeLanguageMetadataCommand =>
        normalizeLanguageMetadataCommand ??= new AsyncRelayCommand(NormalizeLanguageMetadataAsync);
    public IAsyncRelayCommand<LibraryColumnChoiceViewModel> ToggleColumnCommand =>
        toggleColumnCommand ??= new AsyncRelayCommand<LibraryColumnChoiceViewModel>(ToggleColumnAsync);
    public IAsyncRelayCommand<LibraryColumnChoiceViewModel> MoveColumnUpCommand =>
        moveColumnUpCommand ??= new AsyncRelayCommand<LibraryColumnChoiceViewModel>(MoveColumnUpAsync);
    public IAsyncRelayCommand<LibraryColumnChoiceViewModel> MoveColumnDownCommand =>
        moveColumnDownCommand ??= new AsyncRelayCommand<LibraryColumnChoiceViewModel>(MoveColumnDownAsync);
    public IAsyncRelayCommand ResetCurrentViewLayoutCommand =>
        resetCurrentViewLayoutCommand ??= new AsyncRelayCommand(ResetCurrentViewLayoutAsync);
    public IAsyncRelayCommand CopyCurrentViewCommand =>
        copyCurrentViewCommand ??= new AsyncRelayCommand(CopyCurrentViewAsync, CanCopyCurrentView);
    public IAsyncRelayCommand RenameCurrentViewCommand =>
        renameCurrentViewCommand ??= new AsyncRelayCommand(RenameCurrentViewAsync, CanManageCurrentView);
    public IAsyncRelayCommand DeleteCurrentViewCommand =>
        deleteCurrentViewCommand ??= new AsyncRelayCommand(DeleteCurrentViewAsync, CanManageCurrentView);
    public IAsyncRelayCommand ShowMetadataMultiEditCommand =>
        showMetadataMultiEditCommand ??= new AsyncRelayCommand(ShowMetadataMultiEditAsync, () => CanMultiEditSelectedBooks);

    private AsyncRelayCommand? refreshCommand;
    private AsyncRelayCommand? addBooksCommand;
    private AsyncRelayCommand? scanFolderCommand;
    private AsyncRelayCommand? createLibraryCommand;
    private AsyncRelayCommand? openLibraryCommand;
    private RelayCommand? cancelImportCommand;
    private AsyncRelayCommand? showImportDetailsCommand;
    private AsyncRelayCommand? showImportHistoryCommand;
    private AsyncRelayCommand? showDuplicateCandidatesCommand;
    private AsyncRelayCommand? showDuplicateExclusionsCommand;
    private AsyncRelayCommand? showMetadataQualityDashboardCommand;
    private RelayCommand? closeImportJobCommand;
    private AsyncRelayCommand? addGroupingCommand;
    private AsyncRelayCommand<LibraryGroupOption>? removeGroupingCommand;
    private AsyncRelayCommand<FacetFilterViewModel>? renameAuthorFilterCommand;
    private AsyncRelayCommand<FacetFilterViewModel>? removeAuthorFilterCommand;
    private AsyncRelayCommand<FacetFilterViewModel>? renameSeriesFilterCommand;
    private AsyncRelayCommand<FacetFilterViewModel>? removeSeriesFilterCommand;
    private AsyncRelayCommand<FacetFilterViewModel>? renameTagFilterCommand;
    private AsyncRelayCommand<FacetFilterViewModel>? removeTagFilterCommand;
    private AsyncRelayCommand<FacetFilterViewModel>? renameLanguageFilterCommand;
    private AsyncRelayCommand<FacetFilterViewModel>? removeLanguageFilterCommand;
    private AsyncRelayCommand<FacetFilterViewModel>? renameCustomMetadataFilterCommand;
    private AsyncRelayCommand<FacetFilterViewModel>? removeCustomMetadataFilterCommand;
    private AsyncRelayCommand? normalizeLanguageMetadataCommand;
    private AsyncRelayCommand<LibraryColumnChoiceViewModel>? toggleColumnCommand;
    private AsyncRelayCommand<LibraryColumnChoiceViewModel>? moveColumnUpCommand;
    private AsyncRelayCommand<LibraryColumnChoiceViewModel>? moveColumnDownCommand;
    private AsyncRelayCommand? resetCurrentViewLayoutCommand;
    private AsyncRelayCommand? copyCurrentViewCommand;
    private AsyncRelayCommand? renameCurrentViewCommand;
    private AsyncRelayCommand? deleteCurrentViewCommand;
    private AsyncRelayCommand? showMetadataMultiEditCommand;

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        IsLoadingLibrary = true;
        ResetLoadingLibraryProgress();
        EmptyStateMessage = HasActiveLibrary
            ? "Loading library..."
            : "Create or open a library to get started.";
        try
        {
            await RefreshCustomMetadataDefinitionsAsync(cancellationToken);
            await ApplyDefaultViewAsync(cancellationToken);
            if (currentLibrary is not null &&
                !EnsureActiveLibraryStillExists("Create or open a library to get started."))
            {
                return;
            }

            books = await LoadBooksAsync(cancellationToken);
            await RefreshCustomMetadataValuesAsync(books, cancellationToken);
            RefreshFacetFilters();
            ApplyFilter();
            RefreshLibraryDisplay();
        }
        finally
        {
            IsLoadingLibrary = false;
        }
    }

    public void RefreshLocalizedFilterDisplayNames()
    {
        foreach (var filter in LanguageFilters)
        {
            filter.DisplayText = LanguageDisplayService.DisplayName(filter.Name);
        }

        Details.RefreshLocalizedDisplayNames();
    }

    public async Task RefreshSettingsDependentDisplayAsync(CancellationToken cancellationToken = default)
    {
        if (settingsStore is not null)
        {
            var settings = await settingsStore.LoadAsync(cancellationToken);
            authorSortStrategy = settings.AuthorSortStrategy;
        }

        await RefreshCustomMetadataDefinitionsAsync(cancellationToken);
        await RefreshCustomMetadataValuesAsync(books, cancellationToken);
        RefreshFacetFilters();
        RefreshLocalizedFilterDisplayNames();
        ApplyFilter();
    }

    public async Task RefreshCustomMetadataColumnsAsync(CancellationToken cancellationToken = default)
    {
        await RefreshCustomMetadataDefinitionsAsync(cancellationToken);
        var columnsChanged = PruneUnavailableColumnOptions();
        await RefreshCustomMetadataValuesAsync(books, cancellationToken);
        RefreshActiveColumnOptions();
        RefreshColumnChoices();
        OnPropertyChanged(nameof(ActiveColumnLayoutSnapshot));
        if (SelectedBook is { } selectedBook)
        {
            await Details.LoadCustomMetadataValuesAsync(selectedBook.Id, cancellationToken);
        }

        ApplyFilter();
        if (columnsChanged)
        {
            await SaveColumnSettingsAsync(cancellationToken);
        }
    }

    partial void OnLoadingLibraryTotalCountChanged(int value)
    {
        OnPropertyChanged(nameof(LoadingLibraryProgressValue));
        OnPropertyChanged(nameof(IsLoadingLibraryProgressIndeterminate));
        OnPropertyChanged(nameof(LoadingLibraryProgressText));
    }

    partial void OnLoadedLibraryCountChanged(int value)
    {
        OnPropertyChanged(nameof(LoadingLibraryProgressValue));
        OnPropertyChanged(nameof(LoadingLibraryProgressText));
    }

    partial void OnSearchTextChanged(string value) => ApplyFilterUnlessSuppressed();

    partial void OnSelectedViewChanged(LibraryView value)
    {
        if (isApplyingViewDefinition)
        {
            return;
        }

        ActiveViewLayoutKey = value.ToString();
        SelectedViewDefinitionId = value.ToString();
        NotifyViewDefinitionCommandStateChanged();
        RefreshActiveGroupOptions(notifyActiveViewSources: false);
        RefreshActiveColumnOptions();
        if (ApplySelectedViewSortOption())
        {
            ApplyFilter();
        }
        else
        {
            RefreshGroupingOnly();
        }
    }

    partial void OnSelectedViewDefinitionIdChanged(string value)
    {
        var definition = ViewDefinitions.FirstOrDefault(view => view.Id.Equals(value, StringComparison.OrdinalIgnoreCase));
        if (definition is null)
        {
            return;
        }

        ActiveViewLayoutKey = definition.LayoutKey;
        if (SelectedView != definition.BaseView)
        {
            isApplyingViewDefinition = true;
            try
            {
                SelectedView = definition.BaseView;
            }
            finally
            {
                isApplyingViewDefinition = false;
            }

            ActiveViewLayoutKey = definition.LayoutKey;
        }

        NotifyViewDefinitionCommandStateChanged();
        RefreshActiveGroupOptions(notifyActiveViewSources: false);
        RefreshActiveColumnOptions();
        if (ApplySelectedViewSortOption())
        {
            ApplyFilter();
        }
        else
        {
            RefreshGroupingOnly();
        }
    }

    partial void OnSelectedSortOptionChanged(LibrarySortOption value)
    {
        if (isApplyingViewSortOption)
        {
            return;
        }

        SetSortOption(SelectedLayoutKey, value);
        pendingSortSettingsSave = SaveSortSettingsBestEffortAsync(CancellationToken.None);
        ApplyFilter();
    }

    partial void OnSelectedGroupOptionToAddChanged(LibraryGroupOption value)
    {
        addGroupingCommand?.NotifyCanExecuteChanged();
    }

    async partial void OnSelectedBookChanged(BookRowViewModel? value)
    {
        var version = ++selectionVersion;
        if (value is null)
        {
            Details.Clear();
            return;
        }

        Details.Load(value.Book, CurrentLibraryPath);
        var fullBook = await bookRepository.GetAsync(value.Id, CancellationToken.None);
        if (version == selectionVersion && fullBook is not null)
        {
            Details.Load(fullBook, CurrentLibraryPath);
            await Details.LoadFormatDetailsAsync(fullBook.Id, CancellationToken.None);
            await Details.LoadCustomMetadataValuesAsync(fullBook.Id, CancellationToken.None);
        }
    }

    private async Task AddBooksAsync(CancellationToken cancellationToken)
    {
        if (!EnsureActiveLibraryStillExists("Create or open a library before adding books."))
        {
            return;
        }

        var paths = await userInteraction.PickBookFilesAsync(cancellationToken);
        if (paths.Count == 0 || (importService is null && importAgent is null))
        {
            return;
        }

        await ImportFilesAsync(paths, cancellationToken);
    }

    public async Task ImportFilesAsync(
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken = default,
        ImportRunContext? context = null)
    {
        if (!EnsureActiveLibraryStillExists("Create or open a library before adding books."))
        {
            return;
        }

        if (paths.Count == 0 || (importService is null && importAgent is null))
        {
            return;
        }

        if (importAgent is not null)
        {
            await importAgent.StartImportAsync(paths, OnImportProgressAsync, cancellationToken, context ?? ImportRunContext.FileImport);
            OnPropertyChanged(nameof(HasActiveImport));
            return;
        }

        var result = await importService!.ImportAsync(paths, progress: null, cancellationToken, context ?? ImportRunContext.FileImport);
        LastImportResult = CreateImportResultViewModel(result);
        await userInteraction.ShowImportResultAsync(LastImportResult, cancellationToken);
        await RefreshAsync(cancellationToken);
    }

    private async Task ScanFolderAsync(CancellationToken cancellationToken)
    {
        if (!EnsureActiveLibraryStillExists("Create or open a library before scanning folders."))
        {
            return;
        }

        if (directoryScanner is null || (importService is null && importAgent is null))
        {
            return;
        }

        var folder = await userInteraction.PickScanFolderAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        importAgent?.StartScanning();
        var includeSubdirectories = settingsStore is null ||
            (await settingsStore.LoadAsync(cancellationToken)).IncludeScanSubdirectories;
        var files = await Task.Run(
            () => directoryScanner.Scan(folder, includeSubdirectories, cancellationToken),
            cancellationToken);
        await ImportFilesAsync(
            files,
            cancellationToken,
            new ImportRunContext(ImportRunKind.DirectoryScan, folder, includeSubdirectories));
    }

    private async Task<IReadOnlyList<Book>> LoadBooksAsync(CancellationToken cancellationToken)
    {
        var progress = new Progress<LibraryLoadProgress>(snapshot =>
        {
            LoadingLibraryTotalCount = snapshot.TotalCount;
            LoadedLibraryCount = snapshot.LoadedCount;
        });

        return await Task.Run(
            () => LoadBooksInBackgroundAsync(progress, cancellationToken),
            cancellationToken);
    }

    private async Task<IReadOnlyList<Book>> LoadBooksInBackgroundAsync(
        IProgress<LibraryLoadProgress> progress,
        CancellationToken cancellationToken)
    {
        if (HasActiveLibrary && bookRepository is IBookPagedRepository pagedRepository)
        {
            var totalCount = await pagedRepository.CountAsync(cancellationToken);
            progress.Report(new LibraryLoadProgress(totalCount, 0));
            if (totalCount == 0)
            {
                return [];
            }

            var loadedBooks = new List<Book>(totalCount);
            for (var skip = 0; skip < totalCount; skip += LibraryLoadPageSize)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var page = await pagedRepository.ListPageAsync(
                    skip,
                    LibraryLoadPageSize,
                    cancellationToken);
                if (page.Count == 0)
                {
                    break;
                }

                loadedBooks.AddRange(page);
                progress.Report(new LibraryLoadProgress(totalCount, loadedBooks.Count));
            }

            return loadedBooks.AsReadOnly();
        }

        var allBooks = await bookRepository.ListAsync(cancellationToken);
        progress.Report(new LibraryLoadProgress(allBooks.Count, allBooks.Count));
        return allBooks;
    }

    private async Task RefreshCustomMetadataDefinitionsAsync(CancellationToken cancellationToken)
    {
        if (customMetadataRepository is null || !HasActiveLibrary)
        {
            customMetadataFieldDefinitions = [];
            customMetadataFieldDefinitionMap = new Dictionary<Guid, CustomMetadataFieldDefinition>();
            return;
        }

        customMetadataFieldDefinitions = await customMetadataRepository.ListDefinitionsAsync(cancellationToken);
        customMetadataFieldDefinitionMap = customMetadataFieldDefinitions.ToDictionary(field => field.Id);
        RefreshActiveColumnOptions();
        OnPropertyChanged(nameof(ActiveColumnLayoutSnapshot));
    }

    private async Task RefreshCustomMetadataValuesAsync(
        IReadOnlyList<Book> sourceBooks,
        CancellationToken cancellationToken)
    {
        if (customMetadataRepository is null || sourceBooks.Count == 0)
        {
            customMetadataValuesByBookId = [];
            return;
        }

        var values = await customMetadataRepository.GetValuesForBooksAsync(
            sourceBooks.Select(book => book.Id).Distinct().ToArray(),
            cancellationToken);
        customMetadataValuesByBookId = values
            .GroupBy(value => value.BookId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyDictionary<Guid, string>)group
                    .Where(value => customMetadataFieldDefinitionMap.ContainsKey(value.FieldId))
                    .ToDictionary(
                        value => value.FieldId,
                        value => FormatCustomMetadataValue(
                            customMetadataFieldDefinitionMap[value.FieldId].Type,
                            value)));
    }

    private async Task RefreshCustomMetadataValuesForBookAsync(
        Guid bookId,
        CancellationToken cancellationToken)
    {
        if (customMetadataRepository is null)
        {
            customMetadataValuesByBookId.Remove(bookId);
            return;
        }

        var values = await customMetadataRepository.GetValuesAsync(bookId, cancellationToken);
        var formattedValues = values
            .Where(value => customMetadataFieldDefinitionMap.ContainsKey(value.FieldId))
            .ToDictionary(
                value => value.FieldId,
                value => FormatCustomMetadataValue(
                    customMetadataFieldDefinitionMap[value.FieldId].Type,
                    value));
        if (formattedValues.Count == 0)
        {
            customMetadataValuesByBookId.Remove(bookId);
            return;
        }

        customMetadataValuesByBookId[bookId] = formattedValues;
    }

    private async Task RefreshCustomMetadataValuesForBooksAsync(
        IReadOnlyCollection<Guid> bookIds,
        CancellationToken cancellationToken)
    {
        if (customMetadataRepository is null || bookIds.Count == 0)
        {
            return;
        }

        var distinctBookIds = bookIds.Distinct().ToArray();
        var values = await customMetadataRepository.GetValuesForBooksAsync(distinctBookIds, cancellationToken);
        var valuesByBook = values
            .Where(value => customMetadataFieldDefinitionMap.ContainsKey(value.FieldId))
            .GroupBy(value => value.BookId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyDictionary<Guid, string>)group.ToDictionary(
                    value => value.FieldId,
                    value => FormatCustomMetadataValue(
                        customMetadataFieldDefinitionMap[value.FieldId].Type,
                        value)));

        foreach (var bookId in distinctBookIds)
        {
            if (valuesByBook.TryGetValue(bookId, out var formattedValues) &&
                formattedValues.Count > 0)
            {
                customMetadataValuesByBookId[bookId] = formattedValues;
                continue;
            }

            customMetadataValuesByBookId.Remove(bookId);
        }
    }

    private IReadOnlyDictionary<Guid, string> GetCustomMetadataValues(Guid bookId) =>
        customMetadataValuesByBookId.TryGetValue(bookId, out var values)
            ? values
            : new Dictionary<Guid, string>();

    private string FormatCustomMetadataValue(
        CustomMetadataFieldType type,
        CustomMetadataValue value) =>
        type switch
        {
            CustomMetadataFieldType.Number => value.NumberValue?.ToString("0.#############################", CultureInfo.CurrentCulture) ?? string.Empty,
            CustomMetadataFieldType.Date => value.DateValue?.ToString("d", CultureInfo.CurrentCulture) ?? string.Empty,
            CustomMetadataFieldType.Boolean => value.BooleanValue is null
                ? string.Empty
                : localize(value.BooleanValue.Value ? "Yes" : "No"),
            _ => value.TextValue ?? string.Empty
        };

    private void ResetLoadingLibraryProgress()
    {
        LoadingLibraryTotalCount = 0;
        LoadedLibraryCount = 0;
    }

    private sealed record LibraryLoadProgress(int TotalCount, int LoadedCount);

    private async Task CreateLibraryAsync(CancellationToken cancellationToken)
    {
        if (libraryService is null || currentLibrary is null || databaseInitializer is null)
        {
            return;
        }

        var directoryPath = await userInteraction.PickLibraryDirectoryAsync("Create ELibrary", cancellationToken);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return;
        }

        var selectedDirectoryName = Path.GetFileName(Path.TrimEndingDirectorySeparator(directoryPath));
        var libraryRoot = string.Equals(selectedDirectoryName, "ELibrary", StringComparison.OrdinalIgnoreCase)
            ? directoryPath
            : Path.Combine(directoryPath, "ELibrary");
        var library = await libraryService.CreateAsync("ELibrary", libraryRoot, cancellationToken);
        currentLibrary.Set(library);
        RefreshLibraryDisplay();
        await databaseInitializer.InitializeAsync(library, cancellationToken);
        await RefreshAsync(cancellationToken);
    }

    private async Task OpenLibraryAsync(CancellationToken cancellationToken)
    {
        if (libraryService is null || currentLibrary is null || databaseInitializer is null)
        {
            return;
        }

        var directoryPath = await userInteraction.PickLibraryDirectoryAsync("Open library", cancellationToken);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return;
        }

        var library = await libraryService.OpenAsync(directoryPath, cancellationToken);
        currentLibrary.Set(library);
        RefreshLibraryDisplay();
        await databaseInitializer.InitializeAsync(library, cancellationToken);
        await RefreshAsync(cancellationToken);
    }

    private void ApplyFilter()
    {
        var performance = new LibraryViewPerformanceTracker("ApplyFilter");
        var selectedId = SelectedBook?.Id;
        var selectedIds = SelectedBooks.Select(row => row.Id).ToHashSet();
        var filteredBooks = performance.Measure(
            "filter",
            () => ApplyFacetFilters(searchService.Filter(
                books,
                SearchText,
                book => GetCustomMetadataValues(book.Id).Values)));
        var rows = performance.Measure(
            "materialize-sort",
            () => ApplySort(
                    filteredBooks.Select(book => new BookRowViewModel(
                        book,
                        SearchText,
                        CurrentLibraryPath,
                        authorSortStrategy,
                        GetCustomMetadataValues(book.Id))),
                    SelectedSortOption,
                    authorSortStrategy)
                .ToList());

        performance.Measure("visible-reset", () => VisibleBooks.ReplaceAll(rows));

        performance.Measure("grouping", () => RefreshGroupedLibraryNodes(rows));
        OnPropertyChanged(nameof(VisibleBookCount));
        OnPropertyChanged(nameof(IsBookshelfGrouped));
        OnPropertyChanged(nameof(IsLibraryGrouped));
        NotifyActiveViewSourcesChanged();
        performance.Measure(
            "selection",
            () => SelectedBook = selectedId is null
                ? VisibleBooks.FirstOrDefault()
                : VisibleBooks.FirstOrDefault(row => row.Id == selectedId.Value));
        performance.Measure(
            "multi-selection",
            () => SetSelectedBooks(VisibleBooks.Where(row => selectedIds.Contains(row.Id))));
        EmptyStateMessage = HasActiveLibrary
            ? "This library is empty. Add books or scan a folder to begin."
            : "Create or open a library to get started.";
        ReportPerformance(performance, rows.Count);
    }

    private void ApplyFilterUnlessSuppressed()
    {
        if (!isSuppressingFilterRefresh)
        {
            ApplyFilter();
        }
    }

    public void SetSelectedBooks(IEnumerable<BookRowViewModel> selectedRows)
    {
        var visibleIds = VisibleBooks.Select(visible => visible.Id).ToHashSet();
        var rows = selectedRows
            .Where(row => visibleIds.Contains(row.Id))
            .GroupBy(row => row.Id)
            .Select(group => group.First())
            .ToList();
        if (SelectedBooks.Select(row => row.Id).SequenceEqual(rows.Select(row => row.Id)))
        {
            return;
        }

        SelectedBooks.Clear();
        foreach (var row in rows)
        {
            SelectedBooks.Add(row);
        }

        OnPropertyChanged(nameof(SelectedBookCount));
        OnPropertyChanged(nameof(CanMultiEditSelectedBooks));
        OnPropertyChanged(nameof(MetadataMultiEditMenuHeader));
        showMetadataMultiEditCommand?.NotifyCanExecuteChanged();
    }

    public void SetGroupingOptions(IEnumerable<LibraryGroupOption> options)
    {
        var performance = new LibraryViewPerformanceTracker("SetGroupingOptions");
        performance.Measure(
            "active-groups",
            () =>
            {
                viewGroupings[SelectedLayoutKey] = NormalizeGroupOptions(options).ToList();
                RefreshActiveGroupOptions(notifyActiveViewSources: false);
            });
        var visibleCount = RefreshGroupingOnly(performance);
        performance.Measure("settings-schedule", QueueGroupingSettingsSave);
        ReportPerformance(performance, visibleCount);
    }

    private IReadOnlyList<LibraryGroupOption> GetActiveGroupOptions() =>
        ActiveGroupOptions.ToArray();

    private async Task AddGroupingAsync(CancellationToken cancellationToken)
    {
        if (!CanAddGrouping())
        {
            return;
        }

        var performance = new LibraryViewPerformanceTracker("AddGrouping");
        performance.Measure(
            "active-groups",
            () =>
            {
                GetGroupings(SelectedLayoutKey).Add(SelectedGroupOptionToAdd);
                RefreshActiveGroupOptions(notifyActiveViewSources: false);
                SelectNextAvailableGroupOption();
            });
        var visibleCount = RefreshGroupingOnly(performance);
        performance.Measure("settings-schedule", QueueGroupingSettingsSave);
        ReportPerformance(performance, visibleCount);
        await Task.CompletedTask;
    }

    private bool CanAddGrouping() =>
        SelectedGroupOptionToAdd != LibraryGroupOption.None &&
        !ActiveGroupOptions.Contains(SelectedGroupOptionToAdd);

    private async Task RemoveGroupingAsync(LibraryGroupOption option, CancellationToken cancellationToken)
    {
        if (option == LibraryGroupOption.None)
        {
            return;
        }

        var performance = new LibraryViewPerformanceTracker("RemoveGrouping");
        performance.Measure(
            "active-groups",
            () =>
            {
                viewGroupings[SelectedLayoutKey] = GetGroupings(SelectedLayoutKey)
                    .Where(existing => existing != option)
                    .ToList();
                RefreshActiveGroupOptions(notifyActiveViewSources: false);
            });
        var visibleCount = RefreshGroupingOnly(performance);
        performance.Measure("settings-schedule", QueueGroupingSettingsSave);
        ReportPerformance(performance, visibleCount);
        await Task.CompletedTask;
    }

    private void RefreshActiveGroupOptions(bool notifyActiveViewSources = true)
    {
        var desiredOptions = NormalizeGroupOptions(GetGroupings(SelectedLayoutKey));
        for (var index = ActiveGroupOptions.Count - 1; index >= 0; index--)
        {
            if (!desiredOptions.Contains(ActiveGroupOptions[index]))
            {
                ActiveGroupOptions.RemoveAt(index);
            }
        }

        for (var desiredIndex = 0; desiredIndex < desiredOptions.Count; desiredIndex++)
        {
            var option = desiredOptions[desiredIndex];
            var currentIndex = ActiveGroupOptions.IndexOf(option);
            if (currentIndex < 0)
            {
                ActiveGroupOptions.Insert(desiredIndex, option);
            }
            else if (currentIndex != desiredIndex)
            {
                ActiveGroupOptions.Move(currentIndex, desiredIndex);
            }
        }

        addGroupingCommand?.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(IsBookshelfGrouped));
        OnPropertyChanged(nameof(IsLibraryGrouped));
        if (notifyActiveViewSources)
        {
            NotifyActiveViewSourcesChanged();
        }
    }

    private void SelectNextAvailableGroupOption()
    {
        if (CanAddGrouping())
        {
            return;
        }

        var nextOption = AvailableGroupOptions.FirstOrDefault(option =>
            option != LibraryGroupOption.None &&
            !ActiveGroupOptions.Contains(option));
        if (nextOption != LibraryGroupOption.None)
        {
            SelectedGroupOptionToAdd = nextOption;
        }
    }

    private int RefreshGroupingOnly(LibraryViewPerformanceTracker? performanceTracker = null)
    {
        var performance = performanceTracker ?? new LibraryViewPerformanceTracker("RefreshGroupingOnly");
        var rows = performance.Measure("snapshot", () => VisibleBooks.ToArray());
        performance.Measure("grouping", () => RefreshGroupedLibraryNodes(rows));
        OnPropertyChanged(nameof(IsBookshelfGrouped));
        OnPropertyChanged(nameof(IsLibraryGrouped));
        NotifyActiveViewSourcesChanged();
        if (performanceTracker is null)
        {
            ReportPerformance(performance, rows.Length);
        }

        return rows.Length;
    }

    public IReadOnlyList<LibraryColumnKey> GetVisibleColumns(LibraryView view) =>
        GetVisibleColumns(LayoutKeyOrViewKey(view), view);

    private IReadOnlyList<LibraryColumnKey> GetVisibleColumns(string layoutKey, LibraryView baseView) =>
        viewColumns.TryGetValue(layoutKey, out var columns)
            ? columns.ToArray()
            : GetDefaultColumnKeys(baseView);

    public bool IsColumnVisible(LibraryView view, LibraryColumnOption column) =>
        viewColumns.TryGetValue(LayoutKeyOrViewKey(view), out var columns) &&
        columns.Contains(LibraryColumnKey.FromStandard(column));

    public double GetColumnWidth(LibraryView view, LibraryColumnOption column, double defaultWidth) =>
        GetColumnWidth(LayoutKeyOrViewKey(view), LibraryColumnKey.FromStandard(column), defaultWidth);

    public double GetColumnWidth(LibraryView view, LibraryColumnKey column, double defaultWidth) =>
        GetColumnWidth(LayoutKeyOrViewKey(view), column, defaultWidth);

    public string GetColumnHeaderText(LibraryColumnKey key) =>
        key.CustomFieldId is { } fieldId &&
        customMetadataFieldDefinitionMap.TryGetValue(fieldId, out var field)
            ? field.Name
            : localize(GetColumnResourceKey(key.StandardOption ?? LibraryColumnOption.Title));

    private double GetColumnWidth(string layoutKey, LibraryColumnKey column, double defaultWidth) =>
        viewColumnWidths.TryGetValue(layoutKey, out var widths) &&
        widths.TryGetValue(column, out var width) &&
        IsUsableColumnWidth(width)
            ? width
            : defaultWidth;

    public async Task SetColumnWidthAsync(
        LibraryView view,
        LibraryColumnOption column,
        double width,
        CancellationToken cancellationToken = default)
    {
        await SetColumnWidthAsync(LayoutKeyOrViewKey(view), view, LibraryColumnKey.FromStandard(column), width, cancellationToken);
    }

    public async Task SetColumnWidthAsync(
        LibraryView view,
        LibraryColumnKey column,
        double width,
        CancellationToken cancellationToken = default)
    {
        await SetColumnWidthAsync(LayoutKeyOrViewKey(view), view, column, width, cancellationToken);
    }

    private async Task SetColumnWidthAsync(
        string layoutKey,
        LibraryView baseView,
        LibraryColumnKey column,
        double width,
        CancellationToken cancellationToken = default)
    {
        if (baseView == LibraryView.Bookshelf || !IsUsableColumnWidth(width))
        {
            return;
        }

        var roundedWidth = Math.Round(width, 2);
        var widths = GetColumnWidthOptions(layoutKey);

        if (widths.TryGetValue(column, out var existingWidth) &&
            Math.Abs(existingWidth - roundedWidth) < 0.01)
        {
            return;
        }

        widths[column] = roundedWidth;
        await SaveColumnWidthSettingsAsync(cancellationToken);
        OnPropertyChanged(nameof(ActiveColumnLayoutSnapshot));
        NotifyActiveViewSourcesChanged();
    }

    public async Task SetVisibleColumnsAsync(
        LibraryView view,
        IEnumerable<LibraryColumnOption> columns,
        CancellationToken cancellationToken = default)
    {
        if (view == LibraryView.Bookshelf)
        {
            return;
        }

        await SetVisibleColumnsAsync(
            LayoutKeyOrViewKey(view),
            view,
            columns.Select(LibraryColumnKey.FromStandard),
            cancellationToken);
    }

    private async Task SetVisibleColumnsAsync(
        string layoutKey,
        LibraryView baseView,
        IEnumerable<LibraryColumnKey> columns,
        CancellationToken cancellationToken = default)
    {
        if (baseView == LibraryView.Bookshelf)
        {
            return;
        }

        viewColumns[layoutKey] = NormalizeColumnOptions(baseView, columns).ToList();
        if (layoutKey == SelectedLayoutKey)
        {
            RefreshActiveColumnOptions();
            NotifyActiveViewSourcesChanged();
        }

        await SaveColumnSettingsAsync(cancellationToken);
    }

    private void NotifyActiveViewSourcesChanged()
    {
        OnPropertyChanged(nameof(BookshelfVisibleBooksSource));
        OnPropertyChanged(nameof(BookshelfGroupedLibraryNodesSource));
        OnPropertyChanged(nameof(DetailedVisibleBooksSource));
        OnPropertyChanged(nameof(DetailedGroupedLibraryNodesSource));
        OnPropertyChanged(nameof(ListVisibleBooksSource));
        OnPropertyChanged(nameof(ListGroupedLibraryNodesSource));
    }

    public bool HasPendingGroupingSettingsSave => !pendingGroupingSettingsSave.IsCompleted;

    public Task WaitForPendingGroupingSettingsSaveAsync() => pendingGroupingSettingsSave;

    public bool HasPendingSortSettingsSave => !pendingSortSettingsSave.IsCompleted;

    public Task WaitForPendingSortSettingsSaveAsync() => pendingSortSettingsSave;

    private string SelectedLayoutKey => string.IsNullOrWhiteSpace(ActiveViewLayoutKey)
        ? SelectedView.ToString()
        : ActiveViewLayoutKey;

    private static IReadOnlyList<string> BuiltInViewKeys() =>
    [
        nameof(LibraryView.Bookshelf),
        nameof(LibraryView.Detailed),
        nameof(LibraryView.List)
    ];

    private static string ViewKey(LibraryView view) => view.ToString();

    private string LayoutKeyOrViewKey(LibraryView view)
    {
        var selectedDefinition = ViewDefinitions.FirstOrDefault(definition =>
            definition.Id.Equals(SelectedViewDefinitionId, StringComparison.OrdinalIgnoreCase));
        return selectedDefinition is not null && selectedDefinition.BaseView == view
            ? selectedDefinition.LayoutKey
            : ViewKey(view);
    }

    private List<LibraryGroupOption> GetGroupings(string layoutKey)
    {
        if (!viewGroupings.TryGetValue(layoutKey, out var groupings))
        {
            groupings = [];
            viewGroupings[layoutKey] = groupings;
        }

        return groupings;
    }

    private LibrarySortOption GetSortOption(string layoutKey) =>
        viewSortOptions.TryGetValue(layoutKey, out var sortOption)
            ? sortOption
            : LibrarySortOption.None;

    private void SetSortOption(string layoutKey, LibrarySortOption sortOption) =>
        viewSortOptions[layoutKey] = sortOption;

    private List<LibraryColumnKey> GetColumnOptions(string layoutKey, LibraryView baseView)
    {
        if (!viewColumns.TryGetValue(layoutKey, out var columns))
        {
            columns = GetDefaultColumnKeys(baseView).ToList();
            viewColumns[layoutKey] = columns;
        }

        return columns;
    }

    private Dictionary<LibraryColumnKey, double> GetColumnWidthOptions(string layoutKey)
    {
        if (!viewColumnWidths.TryGetValue(layoutKey, out var widths))
        {
            widths = [];
            viewColumnWidths[layoutKey] = widths;
        }

        return widths;
    }

    private void QueueGroupingSettingsSave()
    {
        if (settingsStore is null)
        {
            pendingGroupingSettingsSave = Task.CompletedTask;
            return;
        }

        var saveVersion = Interlocked.Increment(ref groupingSettingsSaveVersion);
        var groupingSettings = CreateGroupingSettings();
        pendingGroupingSettingsSave = Task.Run(
            async () =>
            {
                await settingsSaveLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (saveVersion != Interlocked.Read(ref groupingSettingsSaveVersion))
                    {
                        return;
                    }

                    var settings = await settingsStore.LoadAsync(CancellationToken.None).ConfigureAwait(false);
                    if (saveVersion != Interlocked.Read(ref groupingSettingsSaveVersion))
                    {
                        return;
                    }

                    await settingsStore.SaveAsync(
                            settings with
                            {
                                LibraryGroupings = groupingSettings,
                                LibraryViewLayouts = CreateViewLayoutSettings()
                            },
                            CancellationToken.None)
                        .ConfigureAwait(false);
                }
                finally
                {
                    settingsSaveLock.Release();
                }
            });
    }

    private LibraryGroupingSettings CreateGroupingSettings() =>
        new(
            ToSettingValues(GetGroupings(nameof(LibraryView.Bookshelf))),
            ToSettingValues(GetGroupings(nameof(LibraryView.Detailed))),
            ToSettingValues(GetGroupings(nameof(LibraryView.List))));

    private void LoadGroupingSettings(LibraryGroupingSettings? settings)
    {
        viewGroupings[nameof(LibraryView.Bookshelf)] = ParseGroupOptions(settings?.Bookshelf).ToList();
        viewGroupings[nameof(LibraryView.Detailed)] = ParseGroupOptions(settings?.Detailed).ToList();
        viewGroupings[nameof(LibraryView.List)] = ParseGroupOptions(settings?.List).ToList();
        RefreshActiveGroupOptions();
    }

    private LibrarySortSettings CreateSortSettings() =>
        new(
            GetSortOption(nameof(LibraryView.Bookshelf)).ToString(),
            GetSortOption(nameof(LibraryView.Detailed)).ToString(),
            GetSortOption(nameof(LibraryView.List)).ToString());

    private void LoadSortSettings(LibrarySortSettings? settings)
    {
        viewSortOptions[nameof(LibraryView.Bookshelf)] = ParseSortOption(settings?.Bookshelf);
        viewSortOptions[nameof(LibraryView.Detailed)] = ParseSortOption(settings?.Detailed);
        viewSortOptions[nameof(LibraryView.List)] = ParseSortOption(settings?.List);
        ApplySelectedViewSortOption();
    }

    private LibraryColumnSettings CreateColumnSettings() =>
        new(
            ToColumnSettingValues(GetColumnOptions(nameof(LibraryView.Detailed), LibraryView.Detailed)),
            ToColumnSettingValues(GetColumnOptions(nameof(LibraryView.List), LibraryView.List)));

    private void LoadColumnSettings(LibraryColumnSettings? settings)
    {
        viewColumns[nameof(LibraryView.Bookshelf)] = [];
        viewColumns[nameof(LibraryView.Detailed)] = ParseColumnOptions(LibraryView.Detailed, settings?.Detailed).ToList();
        viewColumns[nameof(LibraryView.List)] = ParseColumnOptions(LibraryView.List, settings?.List).ToList();
        RefreshActiveColumnOptions();
    }

    private LibraryColumnWidthSettings CreateColumnWidthSettings(
        IReadOnlyDictionary<string, double>? duplicateCandidates = null) =>
        new(
            ToColumnWidthSettingValues(GetColumnWidthOptions(nameof(LibraryView.Detailed))),
            ToColumnWidthSettingValues(GetColumnWidthOptions(nameof(LibraryView.List))),
            duplicateCandidates);

    private void LoadColumnWidthSettings(LibraryColumnWidthSettings? settings)
    {
        viewColumnWidths[nameof(LibraryView.Bookshelf)] = [];
        viewColumnWidths[nameof(LibraryView.Detailed)] = ParseColumnWidths(settings?.Detailed);
        viewColumnWidths[nameof(LibraryView.List)] = ParseColumnWidths(settings?.List);
        OnPropertyChanged(nameof(ActiveColumnLayoutSnapshot));
    }

    private LibraryViewLayoutSettings CreateViewLayoutSettings()
    {
        var layouts = new Dictionary<string, LibraryViewLayoutSetting>(StringComparer.Ordinal);
        foreach (var definition in ViewDefinitions)
        {
            layouts[definition.LayoutKey] = CreateViewLayoutSetting(definition.LayoutKey, definition.BaseView);
        }

        foreach (var key in BuiltInViewKeys())
        {
            if (!layouts.ContainsKey(key) &&
                Enum.TryParse<LibraryView>(key, out var baseView))
            {
                layouts[key] = CreateViewLayoutSetting(key, baseView);
            }
        }

        return new(layouts);
    }

    private LibraryViewLayoutSetting CreateViewLayoutSetting(string layoutKey, LibraryView baseView) =>
        baseView == LibraryView.Bookshelf
            ? new(
                Groupings: ToSettingValues(GetGroupings(layoutKey)),
                Sort: GetSortOption(layoutKey).ToString())
            : new(
                Groupings: ToSettingValues(GetGroupings(layoutKey)),
                Columns: ToColumnSettingValues(GetColumnOptions(layoutKey, baseView)),
                ColumnWidths: ToColumnWidthSettingValues(GetColumnWidthOptions(layoutKey)),
                Sort: GetSortOption(layoutKey).ToString());

    private LibraryViewDefinitionSettings CreateViewDefinitionSettings() =>
        new(
            ViewDefinitions
                .Where(definition => !definition.IsBuiltIn)
                .Select(definition => new LibraryViewDefinitionSetting(
                    definition.Id,
                    definition.Name,
                    definition.BaseView.ToString(),
                    definition.LayoutKey))
                .ToArray());

    private void LoadViewLayoutSettings(AppSettings settings)
    {
        RefreshViewDefinitions(settings.LibraryViewDefinitions);

        if (settings.LibraryViewLayouts?.Views?.Count > 0)
        {
            var layouts = settings.LibraryViewLayouts.Views;
            foreach (var definition in ViewDefinitions)
            {
                var layout = GetViewLayout(layouts, definition.LayoutKey) ??
                    GetViewLayout(layouts, definition.BaseView.ToString());
                viewGroupings[definition.LayoutKey] = ParseGroupOptions(layout?.Groupings).ToList();
                viewSortOptions[definition.LayoutKey] = ParseSortOption(layout?.Sort);
                viewColumns[definition.LayoutKey] = definition.BaseView == LibraryView.Bookshelf
                    ? []
                    : ParseColumnOptions(definition.BaseView, layout?.Columns).ToList();
                viewColumnWidths[definition.LayoutKey] = definition.BaseView == LibraryView.Bookshelf
                    ? []
                    : ParseColumnWidths(layout?.ColumnWidths);
            }

            RefreshActiveGroupOptions();
            RefreshActiveColumnOptions();
            ApplySelectedViewSortOption();
            OnPropertyChanged(nameof(ActiveColumnLayoutSnapshot));
            return;
        }

        LoadGroupingSettings(settings.LibraryGroupings);
        LoadColumnSettings(settings.LibraryColumns);
        LoadColumnWidthSettings(settings.LibraryColumnWidths);
        LoadSortSettings(settings.LibrarySorts);
    }

    private void RefreshViewDefinitions(LibraryViewDefinitionSettings? settings)
    {
        ViewDefinitions.Clear();
        ViewDefinitions.Add(new(
            nameof(LibraryView.Bookshelf),
            nameof(LibraryView.Bookshelf),
            LibraryView.Bookshelf,
            nameof(LibraryView.Bookshelf),
            true));
        ViewDefinitions.Add(new(
            nameof(LibraryView.Detailed),
            nameof(LibraryView.Detailed),
            LibraryView.Detailed,
            nameof(LibraryView.Detailed),
            true));
        ViewDefinitions.Add(new(
            nameof(LibraryView.List),
            nameof(LibraryView.List),
            LibraryView.List,
            nameof(LibraryView.List),
            true));

        if (settings?.CustomViews is null)
        {
            return;
        }

        foreach (var customView in settings.CustomViews)
        {
            if (string.IsNullOrWhiteSpace(customView.Id) ||
                string.IsNullOrWhiteSpace(customView.Name) ||
                string.IsNullOrWhiteSpace(customView.LayoutKey) ||
                !Enum.TryParse<LibraryView>(customView.BaseView, ignoreCase: true, out var baseView) ||
                !Enum.IsDefined(baseView))
            {
                continue;
            }

            if (ViewDefinitions.Any(view => view.Id.Equals(customView.Id, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            ViewDefinitions.Add(new(
                customView.Id.Trim(),
                customView.Name.Trim(),
                baseView,
                customView.LayoutKey.Trim(),
                false));
        }
    }

    private static LibraryViewLayoutSetting? GetViewLayout(
        IReadOnlyDictionary<string, LibraryViewLayoutSetting> layouts,
        string layoutKey)
    {
        if (layouts.TryGetValue(layoutKey, out var exactLayout))
        {
            return exactLayout;
        }

        return layouts.TryGetValue(layoutKey.ToLowerInvariant(), out var lowerLayout)
            ? lowerLayout
            : null;
    }

    private void RefreshActiveColumnOptions()
    {
        var desiredOptions = GetVisibleColumns(SelectedLayoutKey, SelectedView);
        for (var index = ActiveColumnOptions.Count - 1; index >= 0; index--)
        {
            if (!desiredOptions.Contains(ActiveColumnOptions[index]))
            {
                ActiveColumnOptions.RemoveAt(index);
            }
        }

        for (var desiredIndex = 0; desiredIndex < desiredOptions.Count; desiredIndex++)
        {
            var option = desiredOptions[desiredIndex];
            var currentIndex = ActiveColumnOptions.IndexOf(option);
            if (currentIndex < 0)
            {
                ActiveColumnOptions.Insert(desiredIndex, option);
            }
            else if (currentIndex != desiredIndex)
            {
                ActiveColumnOptions.Move(currentIndex, desiredIndex);
            }
        }

        RefreshColumnChoices();
        OnPropertyChanged(nameof(ActiveColumnOptionsSnapshot));
        OnPropertyChanged(nameof(ActiveColumnLayoutSnapshot));
    }

    private IReadOnlyDictionary<LibraryColumnKey, double> GetColumnWidths(LibraryView view) =>
        GetColumnWidths(ViewKey(view));

    private IReadOnlyDictionary<LibraryColumnKey, double> GetColumnWidths(string layoutKey) =>
        viewColumnWidths.TryGetValue(layoutKey, out var widths)
            ? new Dictionary<LibraryColumnKey, double>(widths)
            : new Dictionary<LibraryColumnKey, double>();

    private void RefreshColumnChoices()
    {
        var availableOptions = GetAvailableColumnKeys(SelectedView);
        var visibleColumns = GetVisibleColumns(SelectedLayoutKey, SelectedView);
        var visibleOptions = visibleColumns.ToHashSet();
        var orderedOptions = visibleColumns
            .Concat(availableOptions.Where(option => !visibleOptions.Contains(option)))
            .Where(availableOptions.Contains)
            .ToArray();

        for (var index = ColumnChoices.Count - 1; index >= 0; index--)
        {
            if (!orderedOptions.Contains(ColumnChoices[index].Key))
            {
                ColumnChoices.RemoveAt(index);
            }
        }

        for (var desiredIndex = 0; desiredIndex < orderedOptions.Length; desiredIndex++)
        {
            var option = orderedOptions[desiredIndex];
            var choice = ColumnChoices.FirstOrDefault(existing => existing.Key == option);
            if (choice is null)
            {
                ColumnChoices.Insert(
                    desiredIndex,
                    new LibraryColumnChoiceViewModel(option, GetColumnDisplayName(option), visibleOptions.Contains(option)));
                continue;
            }

            choice.DisplayName = GetColumnDisplayName(option);
            choice.IsSelected = visibleOptions.Contains(option);
            var currentIndex = ColumnChoices.IndexOf(choice);
            if (currentIndex != desiredIndex)
            {
                ColumnChoices.Move(currentIndex, desiredIndex);
            }
        }

        OnPropertyChanged(nameof(HasColumnChoices));
    }

    private string GetColumnDisplayName(LibraryColumnKey key)
    {
        if (key.CustomFieldId is { } fieldId &&
            customMetadataFieldDefinitionMap.TryGetValue(fieldId, out var field))
        {
            return field.Name;
        }

        return localize(GetColumnResourceKey(key.StandardOption ?? LibraryColumnOption.Title));
    }

    private static string GetColumnResourceKey(LibraryColumnOption option) =>
        option switch
        {
            LibraryColumnOption.Cover => "Cover",
            LibraryColumnOption.Title => "Title",
            LibraryColumnOption.Authors => "Authors",
            LibraryColumnOption.Format => "Type",
            LibraryColumnOption.Series => "Series",
            LibraryColumnOption.SeriesNumber => "SeriesNumber",
            LibraryColumnOption.Status => "Status",
            LibraryColumnOption.Language => "Language",
            LibraryColumnOption.Publisher => "Publisher",
            LibraryColumnOption.PublicationDate => "PublicationDate",
            LibraryColumnOption.Tags => "Tags",
            LibraryColumnOption.Isbn => "Isbn",
            LibraryColumnOption.Description => "Description",
            LibraryColumnOption.DateAdded => "DateAdded",
            LibraryColumnOption.LastModified => "LastModified",
            LibraryColumnOption.EReader => "EReader",
            _ => "Columns"
        };

    private async Task ToggleColumnAsync(LibraryColumnChoiceViewModel? choice, CancellationToken cancellationToken)
    {
        if (choice is null || SelectedView == LibraryView.Bookshelf)
        {
            return;
        }

        var columns = GetVisibleColumns(SelectedLayoutKey, SelectedView).ToList();
        if (choice.IsSelected)
        {
            if (!columns.Contains(choice.Key))
            {
                columns.Add(choice.Key);
            }
        }
        else
        {
            columns.Remove(choice.Key);
        }

        if (columns.Count == 0)
        {
            choice.IsSelected = true;
            return;
        }

        await SetVisibleColumnsAsync(SelectedLayoutKey, SelectedView, columns, cancellationToken);
    }

    private Task MoveColumnUpAsync(LibraryColumnChoiceViewModel? choice, CancellationToken cancellationToken) =>
        MoveColumnAsync(choice, -1, cancellationToken);

    private Task MoveColumnDownAsync(LibraryColumnChoiceViewModel? choice, CancellationToken cancellationToken) =>
        MoveColumnAsync(choice, 1, cancellationToken);

    public async Task ReorderColumnChoiceAsync(
        LibraryColumnChoiceViewModel? draggedChoice,
        LibraryColumnChoiceViewModel? targetChoice,
        bool insertAfterTarget = false,
        CancellationToken cancellationToken = default)
    {
        if (draggedChoice is null ||
            SelectedView == LibraryView.Bookshelf ||
            !draggedChoice.IsSelected)
        {
            return;
        }

        var orderedOptions = ColumnChoices.Select(choice => choice.Key).ToList();
        var currentIndex = orderedOptions.IndexOf(draggedChoice.Key);
        if (currentIndex < 0)
        {
            return;
        }

        var targetIndex = targetChoice is null
            ? orderedOptions.Count
            : orderedOptions.IndexOf(targetChoice.Key);
        if (targetIndex < 0 || ReferenceEquals(draggedChoice, targetChoice))
        {
            return;
        }

        orderedOptions.RemoveAt(currentIndex);
        if (currentIndex < targetIndex)
        {
            targetIndex--;
        }

        if (insertAfterTarget && targetChoice is not null)
        {
            targetIndex++;
        }

        targetIndex = Math.Clamp(targetIndex, 0, orderedOptions.Count);
        orderedOptions.Insert(targetIndex, draggedChoice.Key);

        var selectedOptions = ColumnChoices
            .Where(choice => choice.IsSelected)
            .Select(choice => choice.Key)
            .ToHashSet();
        var columns = orderedOptions
            .Where(selectedOptions.Contains)
            .ToList();
        await SetVisibleColumnsAsync(SelectedLayoutKey, SelectedView, columns, cancellationToken);
    }

    private async Task MoveColumnAsync(
        LibraryColumnChoiceViewModel? choice,
        int direction,
        CancellationToken cancellationToken)
    {
        if (choice is null || SelectedView == LibraryView.Bookshelf || !choice.IsSelected)
        {
            return;
        }

        var columns = GetVisibleColumns(SelectedLayoutKey, SelectedView).ToList();
        var currentIndex = columns.IndexOf(choice.Key);
        var newIndex = currentIndex + direction;
        if (currentIndex < 0 || newIndex < 0 || newIndex >= columns.Count)
        {
            return;
        }

        (columns[currentIndex], columns[newIndex]) = (columns[newIndex], columns[currentIndex]);
        await SetVisibleColumnsAsync(SelectedLayoutKey, SelectedView, columns, cancellationToken);
    }

    private async Task ResetCurrentViewLayoutAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref groupingSettingsSaveVersion);
        viewGroupings[SelectedLayoutKey] = [];
        viewSortOptions[SelectedLayoutKey] = LibrarySortOption.None;
        if (SelectedView != LibraryView.Bookshelf)
        {
            viewColumns[SelectedLayoutKey] = GetDefaultColumnKeys(SelectedView).ToList();
            viewColumnWidths[SelectedLayoutKey] = [];
        }

        RefreshActiveGroupOptions(notifyActiveViewSources: false);
        RefreshActiveColumnOptions();
        ApplySelectedViewSortOption();
        ApplyFilter();
        await SaveViewCustomizationSettingsAsync(cancellationToken);
    }

    private bool CanCopyCurrentView() => SelectedView is LibraryView.Detailed or LibraryView.List;

    private bool CanManageCurrentView() =>
        ViewDefinitions.Any(definition =>
            definition.Id.Equals(SelectedViewDefinitionId, StringComparison.OrdinalIgnoreCase) &&
            !definition.IsBuiltIn);

    private void NotifyViewDefinitionCommandStateChanged()
    {
        OnPropertyChanged(nameof(SelectedViewDefinition));
        OnPropertyChanged(nameof(CanManageSelectedViewDefinition));
        copyCurrentViewCommand?.NotifyCanExecuteChanged();
        renameCurrentViewCommand?.NotifyCanExecuteChanged();
        deleteCurrentViewCommand?.NotifyCanExecuteChanged();
    }

    private async Task CopyCurrentViewAsync(CancellationToken cancellationToken)
    {
        if (settingsStore is null || !CanCopyCurrentView())
        {
            return;
        }

        var currentDefinition = ViewDefinitions.FirstOrDefault(definition =>
                definition.Id.Equals(SelectedViewDefinitionId, StringComparison.OrdinalIgnoreCase)) ??
            ViewDefinitions.FirstOrDefault(definition =>
                definition.Id.Equals(SelectedView.ToString(), StringComparison.OrdinalIgnoreCase));
        var currentName = currentDefinition?.Name ?? SelectedView.ToString();
        var defaultName = string.Format(
            CultureInfo.CurrentCulture,
            localize("CopyOfViewName"),
            currentName);
        var name = await userInteraction.PromptTextAsync(
                localize("CopyViewPromptTitle"),
                string.Format(CultureInfo.CurrentCulture, localize("CopyViewPromptMessage"), currentName),
                defaultName,
                cancellationToken);
        name = name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var id = CreateUniqueCustomViewId(name);
        var layoutKey = $"custom:{id}";
        var definition = new LibraryViewDefinitionViewModel(
            id,
            name,
            SelectedView,
            layoutKey,
            IsBuiltIn: false);

        viewGroupings[layoutKey] = GetGroupings(SelectedLayoutKey).ToList();
        viewSortOptions[layoutKey] = GetSortOption(SelectedLayoutKey);
        viewColumns[layoutKey] = GetColumnOptions(SelectedLayoutKey, SelectedView).ToList();
        viewColumnWidths[layoutKey] = new Dictionary<LibraryColumnKey, double>(
            GetColumnWidthOptions(SelectedLayoutKey));

        ViewDefinitions.Add(definition);
        await SaveViewDefinitionSettingsAsync(cancellationToken);
        SelectedViewDefinitionId = definition.Id;
    }

    private async Task RenameCurrentViewAsync(CancellationToken cancellationToken)
    {
        if (settingsStore is null)
        {
            return;
        }

        var index = IndexOfSelectedCustomView();
        if (index < 0)
        {
            return;
        }

        var currentDefinition = ViewDefinitions[index];
        var name = await userInteraction.PromptTextAsync(
            localize("RenameViewPromptTitle"),
            string.Format(CultureInfo.CurrentCulture, localize("RenameViewPromptMessage"), currentDefinition.Name),
            currentDefinition.Name,
            cancellationToken);
        name = name?.Trim();
        if (string.IsNullOrWhiteSpace(name) ||
            string.Equals(name, currentDefinition.Name, StringComparison.CurrentCulture))
        {
            return;
        }

        ViewDefinitions[index] = currentDefinition with { Name = name };
        await SaveViewDefinitionSettingsAsync(cancellationToken);
        SelectedViewDefinitionId = currentDefinition.Id;
    }

    private async Task DeleteCurrentViewAsync(CancellationToken cancellationToken)
    {
        if (settingsStore is null)
        {
            return;
        }

        var index = IndexOfSelectedCustomView();
        if (index < 0)
        {
            return;
        }

        var definition = ViewDefinitions[index];
        if (!await userInteraction.ConfirmDeleteViewAsync(definition.Name, cancellationToken))
        {
            return;
        }

        ViewDefinitions.RemoveAt(index);
        viewGroupings.Remove(definition.LayoutKey);
        viewSortOptions.Remove(definition.LayoutKey);
        viewColumns.Remove(definition.LayoutKey);
        viewColumnWidths.Remove(definition.LayoutKey);

        SelectedViewDefinitionId = definition.BaseView.ToString();
        await SaveViewDefinitionSettingsAsync(cancellationToken);
    }

    private int IndexOfSelectedCustomView()
    {
        for (var index = 0; index < ViewDefinitions.Count; index++)
        {
            var definition = ViewDefinitions[index];
            if (definition.Id.Equals(SelectedViewDefinitionId, StringComparison.OrdinalIgnoreCase) &&
                !definition.IsBuiltIn)
            {
                return index;
            }
        }

        return -1;
    }

    private async Task SaveColumnSettingsAsync(CancellationToken cancellationToken)
    {
        if (settingsStore is null)
        {
            return;
        }

        await settingsSaveLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var settings = await settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
            await settingsStore.SaveAsync(
                    settings with
                    {
                        LibraryColumns = CreateColumnSettings(),
                        LibraryViewLayouts = CreateViewLayoutSettings()
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            settingsSaveLock.Release();
        }
    }

    private async Task SaveColumnWidthSettingsAsync(CancellationToken cancellationToken)
    {
        if (settingsStore is null)
        {
            return;
        }

        await settingsSaveLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var settings = await settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
            await settingsStore.SaveAsync(
                    settings with
                    {
                        LibraryColumnWidths = CreateColumnWidthSettings(settings.LibraryColumnWidths?.DuplicateCandidates),
                        LibraryViewLayouts = CreateViewLayoutSettings()
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            settingsSaveLock.Release();
        }
    }

    private async Task SaveSortSettingsBestEffortAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SaveSortSettingsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("Saving library sort settings was canceled.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to persist library sort settings: {ex}");
        }
    }

    private async Task SaveSortSettingsAsync(CancellationToken cancellationToken)
    {
        if (settingsStore is null)
        {
            return;
        }

        await settingsSaveLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var settings = await settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
            await settingsStore.SaveAsync(
                    settings with
                    {
                        LibrarySorts = CreateSortSettings(),
                        LibraryViewLayouts = CreateViewLayoutSettings()
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            settingsSaveLock.Release();
        }
    }

    private async Task SaveViewCustomizationSettingsAsync(CancellationToken cancellationToken)
    {
        if (settingsStore is null)
        {
            return;
        }

        Interlocked.Increment(ref groupingSettingsSaveVersion);
        await settingsSaveLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var settings = await settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
            await settingsStore.SaveAsync(
                    settings with
                    {
                        LibraryGroupings = CreateGroupingSettings(),
                        LibraryColumns = CreateColumnSettings(),
                        LibraryColumnWidths = CreateColumnWidthSettings(settings.LibraryColumnWidths?.DuplicateCandidates),
                        LibrarySorts = CreateSortSettings(),
                        LibraryViewLayouts = CreateViewLayoutSettings()
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            settingsSaveLock.Release();
        }
    }

    private async Task SaveViewDefinitionSettingsAsync(CancellationToken cancellationToken)
    {
        if (settingsStore is null)
        {
            return;
        }

        Interlocked.Increment(ref groupingSettingsSaveVersion);
        await settingsSaveLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var settings = await settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
            await settingsStore.SaveAsync(
                    settings with
                    {
                        LibraryViewDefinitions = CreateViewDefinitionSettings(),
                        LibraryViewLayouts = CreateViewLayoutSettings()
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            settingsSaveLock.Release();
        }
    }

    private string CreateUniqueCustomViewId(string name)
    {
        var baseId = CreateCustomViewId(name);
        var id = baseId;
        var suffix = 2;
        var existingIds = ViewDefinitions.Select(definition => definition.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        while (existingIds.Contains(id))
        {
            id = $"{baseId}-{suffix}";
            suffix++;
        }

        return id;
    }

    private static string CreateCustomViewId(string name)
    {
        var builder = new StringBuilder();
        foreach (var character in name.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                continue;
            }

            if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        var id = builder.ToString().Trim('-');
        return id.Length == 0 ? "view" : id;
    }

    private static IReadOnlyList<string> ToSettingValues(IEnumerable<LibraryGroupOption> options) =>
        NormalizeGroupOptions(options)
            .Select(option => option.ToString())
            .ToArray();

    private static IReadOnlyList<string> ToColumnSettingValues(IEnumerable<LibraryColumnKey> options) =>
        options.Select(option => option.Value).ToArray();

    private static IReadOnlyDictionary<string, double> ToColumnWidthSettingValues(
        IReadOnlyDictionary<LibraryColumnKey, double> widths) =>
        widths
            .Where(item => IsUsableColumnWidth(item.Value))
            .ToDictionary(item => item.Key.Value, item => Math.Round(item.Value, 2), StringComparer.Ordinal);

    private static IReadOnlyList<LibraryGroupOption> ParseGroupOptions(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return [];
        }

        return NormalizeGroupOptions(values.Select(ParseGroupOption));
    }

    private static LibraryGroupOption ParseGroupOption(string? value) =>
        Enum.TryParse<LibraryGroupOption>(value, ignoreCase: true, out var option) &&
        Enum.IsDefined(option)
            ? option
            : LibraryGroupOption.None;

    private static LibrarySortOption ParseSortOption(string? value) =>
        Enum.TryParse<LibrarySortOption>(value, ignoreCase: true, out var option) &&
        Enum.IsDefined(option)
            ? option
            : LibrarySortOption.None;

    private bool ApplySelectedViewSortOption()
    {
        var sortOption = GetSortOption(SelectedLayoutKey);
        if (SelectedSortOption == sortOption)
        {
            return false;
        }

        isApplyingViewSortOption = true;
        try
        {
            SelectedSortOption = sortOption;
            return true;
        }
        finally
        {
            isApplyingViewSortOption = false;
        }
    }

    private static Dictionary<LibraryColumnKey, double> ParseColumnWidths(
        IReadOnlyDictionary<string, double>? values)
    {
        if (values is null)
        {
            return [];
        }

        var widths = new Dictionary<LibraryColumnKey, double>();
        foreach (var (key, width) in values)
        {
            var columnKey = ParseColumnKey(key);
            if (columnKey is not null && IsUsableColumnWidth(width))
            {
                widths[columnKey] = Math.Round(width, 2);
            }
        }

        return widths;
    }

    private static bool IsUsableColumnWidth(double width) =>
        double.IsFinite(width) && width >= 24 && width <= 2000;

    private static IReadOnlyList<LibraryGroupOption> NormalizeGroupOptions(IEnumerable<LibraryGroupOption> options)
    {
        var normalized = new List<LibraryGroupOption>();
        foreach (var option in options)
        {
            if (option == LibraryGroupOption.None || normalized.Contains(option))
            {
                continue;
            }

            normalized.Add(option);
        }

        return normalized;
    }

    private IReadOnlyList<LibraryColumnKey> ParseColumnOptions(
        LibraryView view,
        IEnumerable<string>? values)
    {
        if (values is null)
        {
            return GetDefaultColumnKeys(view);
        }

        var parsed = values
            .Select(ParseColumnKey)
            .OfType<LibraryColumnKey>()
            .ToArray();
        return NormalizeColumnOptions(view, parsed);
    }

    private IReadOnlyList<LibraryColumnKey> NormalizeColumnOptions(
        LibraryView view,
        IEnumerable<LibraryColumnKey> options)
    {
        var allowed = GetAvailableColumnKeys(view).ToHashSet();
        var normalized = options
            .Where(option => allowed.Contains(option))
            .Distinct()
            .ToList();
        return normalized.Count == 0
            ? GetDefaultColumnKeys(view)
            : normalized;
    }

    private bool PruneUnavailableColumnOptions()
    {
        var changed = false;
        foreach (var definition in ViewDefinitions)
        {
            var layoutKey = definition.LayoutKey;
            if (!viewColumns.TryGetValue(layoutKey, out var columns))
            {
                continue;
            }

            var normalized = NormalizeColumnOptions(definition.BaseView, columns).ToList();
            if (!columns.SequenceEqual(normalized))
            {
                viewColumns[layoutKey] = normalized;
                changed = true;
            }

            if (viewColumnWidths.TryGetValue(layoutKey, out var widths))
            {
                var allowed = normalized.ToHashSet();
                foreach (var key in widths.Keys.Where(key => !allowed.Contains(key)).ToArray())
                {
                    widths.Remove(key);
                    changed = true;
                }
            }
        }

        return changed;
    }

    private static LibraryColumnKey? ParseColumnKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.StartsWith(LibraryColumnKey.CustomPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Guid.TryParse(value[LibraryColumnKey.CustomPrefix.Length..], out var fieldId)
                ? LibraryColumnKey.FromCustom(fieldId)
                : null;
        }

        return Enum.TryParse<LibraryColumnOption>(value, ignoreCase: true, out var option) && Enum.IsDefined(option)
            ? LibraryColumnKey.FromStandard(option)
            : null;
    }

    private IReadOnlyList<LibraryColumnKey> GetAvailableColumnKeys(LibraryView view) =>
        GetDefaultColumnKeys(view)
            .Concat(customMetadataFieldDefinitions.Select(field => LibraryColumnKey.FromCustom(field.Id)))
            .ToArray();

    private static IReadOnlyList<LibraryColumnKey> GetDefaultColumnKeys(LibraryView view) =>
        GetDefaultColumns(view).Select(LibraryColumnKey.FromStandard).ToArray();

    private static IReadOnlyList<LibraryColumnOption> GetDefaultColumns(LibraryView view) =>
        view switch
        {
            LibraryView.Detailed => DefaultDetailedColumns(),
            LibraryView.List => DefaultListColumns(),
            _ => []
        };

    private static IReadOnlyList<LibraryColumnOption> DefaultDetailedColumns() =>
    [
        LibraryColumnOption.Cover,
        LibraryColumnOption.Title,
        LibraryColumnOption.Authors,
        LibraryColumnOption.Format,
        LibraryColumnOption.Series,
        LibraryColumnOption.SeriesNumber,
        LibraryColumnOption.Status,
        LibraryColumnOption.Language,
        LibraryColumnOption.Publisher,
        LibraryColumnOption.PublicationDate,
        LibraryColumnOption.Tags,
        LibraryColumnOption.Isbn,
        LibraryColumnOption.Description,
        LibraryColumnOption.DateAdded,
        LibraryColumnOption.LastModified,
        LibraryColumnOption.EReader
    ];

    private static IReadOnlyList<LibraryColumnOption> DefaultListColumns() =>
    [
        LibraryColumnOption.Title,
        LibraryColumnOption.Authors,
        LibraryColumnOption.Series,
        LibraryColumnOption.SeriesNumber,
        LibraryColumnOption.Status,
        LibraryColumnOption.Language,
        LibraryColumnOption.Format,
        LibraryColumnOption.Publisher,
        LibraryColumnOption.PublicationDate,
        LibraryColumnOption.Tags,
        LibraryColumnOption.Isbn,
        LibraryColumnOption.Description,
        LibraryColumnOption.DateAdded,
        LibraryColumnOption.LastModified,
        LibraryColumnOption.EReader
    ];

    private void RefreshGroupedLibraryNodes(IReadOnlyList<BookRowViewModel> rows)
    {
        var expandedGroupPaths = CaptureExpandedGroupPaths(GroupedLibraryNodes);
        var groupOptions = GetActiveGroupOptions();
        if (groupOptions.Count == 0)
        {
            GroupedLibraryNodes.ReplaceAll([]);
            return;
        }

        GroupedLibraryNodes.ReplaceAll(BuildGroupNodes(rows, groupOptions, level: 0, parentPath: string.Empty, expandedGroupPaths));
    }

    private IEnumerable<LibraryGroupNodeViewModel> BuildGroupNodes(
        IReadOnlyList<BookRowViewModel> rows,
        IReadOnlyList<LibraryGroupOption> groupOptions,
        int level,
        string parentPath,
        ISet<string> expandedGroupPaths)
    {
        if (level >= groupOptions.Count)
        {
            yield break;
        }

        var groupOption = groupOptions[level];
        var groupedRows = rows
            .SelectMany(row => GetDisplayGroupNames(row.Book, groupOption)
                .Select(groupName => new { GroupName = groupName, Row = row }))
            .GroupBy(item => item.GroupName, StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase);

        foreach (var rowGroup in groupedRows)
        {
            var groupRows = rowGroup
                .Select(item => item.Row)
                .DistinctBy(row => row.Id)
                .ToList();
            var isLastGroupLevel = level + 1 >= groupOptions.Count;
            var directBooks = isLastGroupLevel ? groupRows : new List<BookRowViewModel>();
            var childRows = isLastGroupLevel ? new List<BookRowViewModel>() : groupRows;

            if (!isLastGroupLevel &&
                groupOptions[level + 1] == LibraryGroupOption.Series)
            {
                directBooks = groupRows
                    .Where(row => string.IsNullOrWhiteSpace(row.Book.Metadata.Series))
                    .ToList();
                childRows = groupRows
                    .Where(row => !string.IsNullOrWhiteSpace(row.Book.Metadata.Series))
                    .ToList();
            }

            var groupPath = CreateGroupPath(parentPath, groupOptions[level], rowGroup.Key);
            var children = BuildGroupNodes(childRows, groupOptions, level + 1, groupPath, expandedGroupPaths).ToList();
            yield return new LibraryGroupNodeViewModel(
                rowGroup.Key,
                children,
                directBooks,
                groupOptions[level],
                groupRows.Count)
            {
                IsExpanded = expandedGroupPaths.Contains(groupPath)
            };
        }
    }

    private IEnumerable<string> GetDisplayGroupNames(Book book, LibraryGroupOption groupOption) =>
        groupOption switch
        {
            LibraryGroupOption.Author => NonEmptyValues(book.Metadata.Authors, localize("GroupUnknownAuthor")),
            LibraryGroupOption.Series => SingleNonEmptyValue(book.Metadata.Series, localize("GroupNoSeries")),
            LibraryGroupOption.Tag => NonEmptyValues(book.Metadata.Tags ?? [], localize("GroupNoTags")),
            LibraryGroupOption.Language => SingleNonEmptyValue(
                string.IsNullOrWhiteSpace(book.Metadata.Language)
                    ? null
                    : LanguageDisplayService.DisplayName(book.Metadata.Language),
                localize("GroupNoLanguage")),
            LibraryGroupOption.Status => [localize(book.ReadingStatus.ToString())],
            LibraryGroupOption.Format => NonEmptyValues(
                book.Formats.Select(format => format.ToString().ToUpperInvariant()),
                localize("GroupNoFormat")),
            _ => [string.Empty]
        };

    private static ISet<string> CaptureExpandedGroupPaths(IEnumerable<LibraryGroupNodeViewModel> groups) =>
        CaptureExpandedGroupPaths(groups, string.Empty);

    private static ISet<string> CaptureExpandedGroupPaths(
        IEnumerable<LibraryGroupNodeViewModel> groups,
        string parentPath)
    {
        var expandedPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in groups)
        {
            var groupPath = CreateGroupPath(parentPath, group.GroupOption, group.Header);
            if (group.IsExpanded)
            {
                expandedPaths.Add(groupPath);
            }

            expandedPaths.UnionWith(CaptureExpandedGroupPaths(group.Groups, groupPath));
        }

        return expandedPaths;
    }

    private static string CreateGroupPath(string parentPath, LibraryGroupOption option, string header) =>
        string.Concat(parentPath, "\u001F", option, "\u001F", header);

    private static string DefaultGroupText(string key) =>
        key switch
        {
            "GroupUnknownAuthor" => "Unknown author",
            "GroupNoSeries" => "No series",
            "GroupNoTags" => "No tags",
            "GroupNoLanguage" => "No language",
            "GroupNoFormat" => "No format",
            "Unread" => "Unread",
            "Reading" => "Reading",
            "Read" => "Read",
            _ => key
        };

    private static IEnumerable<string> NonEmptyValues(IEnumerable<string> values, string fallback)
    {
        var normalized = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        return normalized.Length == 0 ? [fallback] : normalized;
    }

    private static IEnumerable<string> SingleNonEmptyValue(string? value, string fallback) =>
        [string.IsNullOrWhiteSpace(value) ? fallback : value.Trim()];

    private IReadOnlyList<Book> ApplyFacetFilters(IReadOnlyList<Book> source)
    {
        var selectedFilters = new[]
            {
                (Filters: AuthorFilters, ValueSelector: (Func<Book, IEnumerable<string>>)(book => book.Metadata.Authors)),
                (Filters: CategoryFilters, ValueSelector: (Func<Book, IEnumerable<string>>)(book => book.Metadata.Tags ?? [])),
                (Filters: SeriesFilters, ValueSelector: (Func<Book, IEnumerable<string>>)(book => SingleOptionalValue(book.Metadata.Series))),
                (Filters: StatusFilters, ValueSelector: (Func<Book, IEnumerable<string>>)(book => [book.ReadingStatus.ToString()])),
                (Filters: EReaderFilters, ValueSelector: (Func<Book, IEnumerable<string>>)(book => [new BookRowViewModel(book).EReader])),
                (Filters: LanguageFilters, ValueSelector: (Func<Book, IEnumerable<string>>)(book => SingleOptionalValue(LanguageDisplayService.FilterKey(book.Metadata.Language)))),
                (Filters: FormatFilters, ValueSelector: (Func<Book, IEnumerable<string>>)(book => book.Formats.Select(format => format.ToString())))
            }
            .Select(group => (
                group.ValueSelector,
                Values: group.Filters
                    .Where(filter => filter.IsSelected)
                    .Select(filter => filter.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)))
            .Where(group => group.Values.Count > 0)
            .ToArray();
        var selectedCustomFilters = CustomMetadataFilterGroups
            .Select(group => (
                group.FieldId,
                group.Type,
                Values: group.Filters
                    .Where(filter => filter.IsSelected)
                    .Select(filter => filter.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)))
            .Where(group => group.Values.Count > 0)
            .ToArray();

        if (selectedFilters.Length == 0 && selectedCustomFilters.Length == 0)
        {
            return source;
        }

        return source
            .Where(book =>
                selectedFilters.Any(group => group.ValueSelector(book).Any(group.Values.Contains)) ||
                selectedCustomFilters.Any(group =>
                    GetCustomMetadataValues(book.Id).TryGetValue(group.FieldId, out var value) &&
                    CustomMetadataFilterValues(group.Type, value).Any(group.Values.Contains)))
            .ToList();
    }

    private void RefreshFacetFilters()
    {
        RefreshFilters(
            AuthorFilters,
            books.SelectMany(book => book.Metadata.Authors),
            sortKeySelector: author => AuthorSortKeyBuilder.BuildSortKey(author, authorSortStrategy));
        RefreshFilters(
            CategoryFilters,
            books.SelectMany(book => book.Metadata.Tags ?? []));
        RefreshFilters(
            SeriesFilters,
            books.SelectMany(book => SingleOptionalValue(book.Metadata.Series)));
        RefreshStatusFilters();
        RefreshFilters(
            EReaderFilters,
            books.Select(book => new BookRowViewModel(book).EReader));
        RefreshFilters(
            LanguageFilters,
            books.SelectMany(book => SingleOptionalValue(LanguageDisplayService.FilterKey(book.Metadata.Language))),
            LanguageDisplayService.DisplayName);
        RefreshFilters(
            FormatFilters,
            books.SelectMany(book => book.Formats.Select(format => format.ToString())),
            FormatDisplayName);
        RefreshCustomMetadataFilters();
        ApplyAllFilterSearches();
        NotifyFacetFilterCollectionsChanged();
    }

    private void NotifyFacetFilterCollectionsChanged()
    {
        OnPropertyChanged(nameof(AuthorFilters));
        OnPropertyChanged(nameof(CategoryFilters));
        OnPropertyChanged(nameof(SeriesFilters));
        OnPropertyChanged(nameof(StatusFilters));
        OnPropertyChanged(nameof(EReaderFilters));
        OnPropertyChanged(nameof(LanguageFilters));
        OnPropertyChanged(nameof(FormatFilters));
    }

    partial void OnAuthorFilterSearchTextChanged(string value) =>
        ApplyStandardFilterSearch(
            AuthorFilters,
            value,
            nameof(VisibleAuthorFilterCount),
            nameof(AuthorFilterSearchSummary),
            nameof(HasAuthorFilterSearch));

    partial void OnCategoryFilterSearchTextChanged(string value) =>
        ApplyStandardFilterSearch(
            CategoryFilters,
            value,
            nameof(VisibleCategoryFilterCount),
            nameof(CategoryFilterSearchSummary),
            nameof(HasCategoryFilterSearch));

    partial void OnSeriesFilterSearchTextChanged(string value) =>
        ApplyStandardFilterSearch(
            SeriesFilters,
            value,
            nameof(VisibleSeriesFilterCount),
            nameof(SeriesFilterSearchSummary),
            nameof(HasSeriesFilterSearch));

    partial void OnLanguageFilterSearchTextChanged(string value) =>
        ApplyStandardFilterSearch(
            LanguageFilters,
            value,
            nameof(VisibleLanguageFilterCount),
            nameof(LanguageFilterSearchSummary),
            nameof(HasLanguageFilterSearch));

    partial void OnFormatFilterSearchTextChanged(string value) =>
        ApplyStandardFilterSearch(
            FormatFilters,
            value,
            nameof(VisibleFormatFilterCount),
            nameof(FormatFilterSearchSummary),
            nameof(HasFormatFilterSearch));

    private void ApplyAllFilterSearches()
    {
        ApplyStandardFilterSearch(
            AuthorFilters,
            AuthorFilterSearchText,
            nameof(VisibleAuthorFilterCount),
            nameof(AuthorFilterSearchSummary),
            nameof(HasAuthorFilterSearch));
        ApplyStandardFilterSearch(
            CategoryFilters,
            CategoryFilterSearchText,
            nameof(VisibleCategoryFilterCount),
            nameof(CategoryFilterSearchSummary),
            nameof(HasCategoryFilterSearch));
        ApplyStandardFilterSearch(
            SeriesFilters,
            SeriesFilterSearchText,
            nameof(VisibleSeriesFilterCount),
            nameof(SeriesFilterSearchSummary),
            nameof(HasSeriesFilterSearch));
        ApplyStandardFilterSearch(
            LanguageFilters,
            LanguageFilterSearchText,
            nameof(VisibleLanguageFilterCount),
            nameof(LanguageFilterSearchSummary),
            nameof(HasLanguageFilterSearch));
        ApplyStandardFilterSearch(
            FormatFilters,
            FormatFilterSearchText,
            nameof(VisibleFormatFilterCount),
            nameof(FormatFilterSearchSummary),
            nameof(HasFormatFilterSearch));
        foreach (var group in CustomMetadataFilterGroups)
        {
            group.ApplySearch();
        }
    }

    private void ApplyStandardFilterSearch(
        ObservableCollection<FacetFilterViewModel> filters,
        string? searchText,
        string visibleCountPropertyName,
        string summaryPropertyName,
        string searchVisibilityPropertyName)
    {
        var query = searchText?.Trim();
        foreach (var filter in filters)
        {
            filter.IsVisible = string.IsNullOrWhiteSpace(query) ||
                FilterTextMatches(filter, query);
        }

        OnPropertyChanged(visibleCountPropertyName);
        OnPropertyChanged(summaryPropertyName);
        OnPropertyChanged(searchVisibilityPropertyName);
    }

    private void RefreshCustomMetadataFilters()
    {
        var existingSelections = CustomMetadataFilterGroups
            .SelectMany(group => group.Filters.Select(filter => new
            {
                group.FieldId,
                filter.Name,
                filter.IsSelected
            }))
            .ToDictionary(
                item => (item.FieldId, item.Name),
                item => item.IsSelected);
        var existingSearchTexts = CustomMetadataFilterGroups.ToDictionary(
            group => group.FieldId,
            group => group.FilterSearchText);

        CustomMetadataFilterGroups.Clear();
        foreach (var definition in customMetadataFieldDefinitions
                     .Where(IsUsefulCustomMetadataFilterType)
                     .OrderBy(field => field.SortOrder)
                     .ThenBy(field => field.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            var valueCounts = customMetadataValuesByBookId.Values
                .Select(values => values.TryGetValue(definition.Id, out var value) ? value : null)
                .SelectMany(value => CustomMetadataFilterValues(definition.Type, value))
                .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
                .Select(group => new
                {
                    Name = group.Key,
                    Count = group.Count()
                })
                .OrderBy(value => value.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            if (valueCounts.Count == 0)
            {
                continue;
            }

            var filters = new ObservableCollection<FacetFilterViewModel>();
            foreach (var value in valueCounts)
            {
                var isSelected = existingSelections.TryGetValue((definition.Id, value.Name), out var existingSelection) &&
                    existingSelection;
                filters.Add(new FacetFilterViewModel(
                    value.Name,
                    value.Count,
                    isSelected,
                    ApplyFilterUnlessSuppressed,
                    customMetadataFieldId: definition.Id));
            }

            var group = new CustomMetadataFilterGroupViewModel(definition, filters);
            if (existingSearchTexts.TryGetValue(definition.Id, out var searchText))
            {
                group.FilterSearchText = searchText;
            }
            else
            {
                group.ApplySearch();
            }

            CustomMetadataFilterGroups.Add(group);
        }

        OnPropertyChanged(nameof(HasCustomMetadataFilterGroups));
    }

    private static IEnumerable<string> CustomMetadataFilterValues(CustomMetadataFieldType type, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return type == CustomMetadataFieldType.MultiSelect
            ? value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : [value.Trim()];
    }

    private static bool CanCleanupCustomMetadataValues(CustomMetadataFieldDefinition definition) =>
        definition.Type is
            CustomMetadataFieldType.Text or
            CustomMetadataFieldType.SingleSelect or
            CustomMetadataFieldType.MultiSelect;

    private static bool CustomMetadataValueMatches(
        CustomMetadataFieldType type,
        string value,
        string oldValue) =>
        CustomMetadataFilterValues(type, value)
            .Any(item => string.Equals(item, oldValue, StringComparison.OrdinalIgnoreCase));

    private static bool IsUsefulCustomMetadataFilterType(CustomMetadataFieldDefinition definition) =>
        definition.Type is
            CustomMetadataFieldType.Text or
            CustomMetadataFieldType.SingleSelect or
            CustomMetadataFieldType.MultiSelect or
            CustomMetadataFieldType.Boolean or
            CustomMetadataFieldType.Number or
            CustomMetadataFieldType.Date;

    private void RefreshFilters(
        ObservableCollection<FacetFilterViewModel> filters,
        IEnumerable<string> values,
        Func<string, string>? displayNameSelector = null,
        Func<string, string>? sortKeySelector = null)
    {
        var existingSelections = filters.ToDictionary(
            filter => filter.Name,
            filter => filter.IsSelected,
            StringComparer.OrdinalIgnoreCase);
        var valueCounts = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Name = group.First(),
                Count = group.Count()
            })
            .OrderBy(value => sortKeySelector?.Invoke(value.Name) ?? value.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(value => value.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        filters.Clear();
        foreach (var value in valueCounts)
        {
            var isSelected = existingSelections.TryGetValue(value.Name, out var existingSelection) && existingSelection;
            filters.Add(new FacetFilterViewModel(
                value.Name,
                value.Count,
                isSelected,
                ApplyFilterUnlessSuppressed,
                displayNameSelector?.Invoke(value.Name)));
        }
    }

    private void RefreshStatusFilters()
    {
        var existingSelections = StatusFilters.ToDictionary(
            filter => filter.Name,
            filter => filter.IsSelected,
            StringComparer.OrdinalIgnoreCase);
        var statusCounts = books
            .GroupBy(book => book.ReadingStatus)
            .ToDictionary(group => group.Key, group => group.Count());

        StatusFilters.Clear();
        foreach (var status in Enum.GetValues<ReadingStatus>())
        {
            if (!statusCounts.TryGetValue(status, out var count))
            {
                continue;
            }

            var name = status.ToString();
            var isSelected = existingSelections.TryGetValue(name, out var existingSelection) && existingSelection;
            StatusFilters.Add(new FacetFilterViewModel(name, count, isSelected, ApplyFilterUnlessSuppressed));
        }
    }

    private static IEnumerable<string> SingleOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? [] : [value];

    private static string FormatDisplayName(string value) => value.ToUpperInvariant();

    private static int CountVisibleFilters(IEnumerable<FacetFilterViewModel> filters) =>
        filters.Count(filter => filter.IsVisible);

    private static string FormatFilterSearchCountSummary(int visibleCount, int totalCount) =>
        $"{visibleCount} / {totalCount}";

    private static bool ShouldShowFilterSearch(int totalCount, string? searchText) =>
        totalCount >= FilterSearchMinimumItemCount || !string.IsNullOrWhiteSpace(searchText);

    private static bool FilterTextMatches(FacetFilterViewModel filter, string query) =>
        filter.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
        filter.DisplayText.Contains(query, StringComparison.CurrentCultureIgnoreCase);

    private async Task RenameFilterValueAsync(
        FacetFilterViewModel? filter,
        MetadataFilterKind kind)
    {
        if (filter is null)
        {
            return;
        }

        var newValue = await userInteraction.PromptTextAsync(
            localize("FilterRenameTitle"),
            string.Format(CultureInfo.CurrentCulture, localize("FilterRenameMessage"), filter.Name),
            filter.Name,
            CancellationToken.None);
        if (string.IsNullOrWhiteSpace(newValue) ||
            (kind != MetadataFilterKind.Language &&
                string.Equals(filter.Name, newValue.Trim(), StringComparison.Ordinal)))
        {
            return;
        }

        await ApplyMetadataValueEditAsync(
            kind,
            filter.Name,
            replacementValue: newValue.Trim(),
            remove: false,
            CancellationToken.None);
    }

    private async Task RemoveFilterValueAsync(
        FacetFilterViewModel? filter,
        MetadataFilterKind kind)
    {
        if (filter is null)
        {
            return;
        }

        if (!await userInteraction.ConfirmMetadataValueRemovalAsync(
                filter.Name,
                filter.Count,
                CancellationToken.None))
        {
            return;
        }

        await ApplyMetadataValueEditAsync(
            kind,
            filter.Name,
            replacementValue: null,
            remove: true,
            CancellationToken.None);
    }

    private async Task RenameCustomMetadataFilterValueAsync(FacetFilterViewModel? filter)
    {
        if (filter?.CustomMetadataFieldId is not { } fieldId ||
            !customMetadataFieldDefinitionMap.TryGetValue(fieldId, out var definition) ||
            !CanCleanupCustomMetadataValues(definition))
        {
            return;
        }

        var newValue = await userInteraction.PromptTextAsync(
            localize("FilterRenameTitle"),
            string.Format(CultureInfo.CurrentCulture, localize("FilterRenameMessage"), filter.Name),
            filter.Name,
            CancellationToken.None);
        if (string.IsNullOrWhiteSpace(newValue) ||
            string.Equals(filter.Name, newValue.Trim(), StringComparison.Ordinal))
        {
            return;
        }

        await ApplyCustomMetadataValueEditAsync(
            definition,
            filter.Name,
            replacementValue: newValue.Trim(),
            remove: false,
            CancellationToken.None);
    }

    private async Task RemoveCustomMetadataFilterValueAsync(FacetFilterViewModel? filter)
    {
        if (filter?.CustomMetadataFieldId is not { } fieldId ||
            !customMetadataFieldDefinitionMap.TryGetValue(fieldId, out var definition) ||
            !CanCleanupCustomMetadataValues(definition))
        {
            return;
        }

        if (!await userInteraction.ConfirmMetadataValueRemovalAsync(
                filter.Name,
                filter.Count,
                CancellationToken.None))
        {
            return;
        }

        await ApplyCustomMetadataValueEditAsync(
            definition,
            filter.Name,
            replacementValue: null,
            remove: true,
            CancellationToken.None);
    }

    private async Task ShowMetadataMultiEditAsync(CancellationToken cancellationToken)
    {
        var selectedIds = SelectedBooks.Select(row => row.Id).ToArray();
        if (selectedIds.Length == 0)
        {
            return;
        }

        var edit = new MetadataMultiEditViewModel(selectedIds.Length, customMetadataFieldDefinitions);
        var result = await userInteraction.ShowMetadataMultiEditAsync(edit, cancellationToken);
        if (result is null)
        {
            return;
        }

        await ApplyMetadataMultiEditAsync(selectedIds, result, cancellationToken);
    }

    private async Task ApplyMetadataMultiEditAsync(
        IReadOnlyCollection<Guid> selectedIds,
        MetadataMultiEditResult result,
        CancellationToken cancellationToken)
    {
        IsCleaningMetadata = true;
        MetadataCleanupStatusText = "Updating metadata...";
        await Task.Yield();
        try
        {
            var persistedBooks = new List<Book>();
            var customChanges = TryCreateCustomMetadataMultiEditChanges(result);
            if (customChanges is null)
            {
                await userInteraction.ShowMessageAsync(
                    localize("MetadataMultiEditTitle"),
                    MetadataCleanupStatusText,
                    cancellationToken);
                return;
            }

            var customMetadataChanged = false;
            var skippedBookCount = 0;
            foreach (var bookId in selectedIds)
            {
                var fullBook = await bookRepository.GetAsync(bookId, cancellationToken);
                if (fullBook is not null)
                {
                    var updatedBook = ApplyMetadataMultiEdit(fullBook, result);
                    if (updatedBook != fullBook)
                    {
                        try
                        {
                            await bookRepository.UpdateAsync(updatedBook, cancellationToken);
                            persistedBooks.Add(updatedBook);
                        }
                        catch (BookConflictException)
                        {
                            // Keep conflicting books unchanged; later slices can expose skipped books in the dialog.
                            skippedBookCount++;
                        }
                    }

                    if (customChanges.Count > 0 &&
                        customMetadataRepository is not null &&
                        await ApplyCustomMetadataMultiEditAsync(bookId, customChanges, cancellationToken))
                    {
                        customMetadataChanged = true;
                    }
                }
            }

            if (customMetadataChanged)
            {
                await RefreshCustomMetadataValuesForBooksAsync(selectedIds, cancellationToken);
                if (SelectedBook is { } selectedBook &&
                    selectedIds.Contains(selectedBook.Id))
                {
                    await Details.LoadCustomMetadataValuesAsync(selectedBook.Id, cancellationToken);
                }
            }

            if (persistedBooks.Count > 0)
            {
                ApplyPersistedMetadataChanges(persistedBooks, refreshDisplay: !customMetadataChanged);
            }

            if (customMetadataChanged)
            {
                RefreshFacetFilters();
                ApplyFilter();
            }

            if (skippedBookCount > 0)
            {
                await userInteraction.ShowMessageAsync(
                    localize("MetadataMultiEditTitle"),
                    string.Format(
                        CultureInfo.CurrentCulture,
                        localize("MetadataMultiEditSkippedConflicts"),
                        skippedBookCount),
                    cancellationToken);
            }
        }
        finally
        {
            IsCleaningMetadata = false;
        }
    }

    private static Book ApplyMetadataMultiEdit(Book book, MetadataMultiEditResult result)
    {
        var metadata = book.Metadata;
        var title = metadata.Title;
        var authors = result.UpdateAuthors ? SplitRequiredList(result.AuthorsText) : metadata.Authors;
        if (result.SwapTitleAndAuthors)
        {
            title = string.Join(", ", metadata.Authors).Trim();
            authors = [metadata.Title.Trim()];
        }

        var tags = result.UpdateTags ? ApplyTagAction(metadata.Tags, result.TagAction, result.TagsText) : metadata.Tags;
        var series = result.UpdateSeries ? NormalizeBlank(result.SeriesText) : metadata.Series;
        var seriesNumber = result.UpdateSeries && series is null ? null : metadata.SeriesNumber;
        var language = result.UpdateLanguage ? NormalizeBlank(result.LanguageText) : metadata.Language;
        var readingStatus = result.UpdateStatus ? result.Status : book.ReadingStatus;

        if (title == metadata.Title &&
            authors.SequenceEqual(metadata.Authors) &&
            NullableSequenceEqual(tags, metadata.Tags) &&
            series == metadata.Series &&
            seriesNumber == metadata.SeriesNumber &&
            language == metadata.Language &&
            readingStatus == book.ReadingStatus)
        {
            return book;
        }

        return book with
        {
            Metadata = CopyMetadata(metadata, title, authors, tags, series, language, seriesNumber),
            ReadingStatus = readingStatus,
            UpdatedUtc = DateTimeOffset.UtcNow
        };
    }

    private IReadOnlyList<CustomMetadataValueChange>? TryCreateCustomMetadataMultiEditChanges(
        MetadataMultiEditResult result)
    {
        try
        {
            return (result.CustomFields ?? [])
                .Select(field => string.IsNullOrWhiteSpace(field.ValueText)
                    ? new CustomMetadataValueChange(Guid.Empty, field.FieldId, null)
                    : new CustomMetadataValueChange(Guid.Empty, field.FieldId, CreateCustomMetadataValue(Guid.Empty, field)))
                .ToArray();
        }
        catch (FormatException exception)
        {
            MetadataCleanupStatusText = CustomMetadataValueParser.TryFormatValidationMessage(exception, localize, out var message)
                ? message
                : exception.Message;
            return null;
        }
        catch (InvalidOperationException exception)
        {
            MetadataCleanupStatusText = CustomMetadataValueParser.TryFormatValidationMessage(exception, localize, out var message)
                ? message
                : exception.Message;
            return null;
        }
    }

    private async Task<bool> ApplyCustomMetadataMultiEditAsync(
        Guid bookId,
        IReadOnlyList<CustomMetadataValueChange> changes,
        CancellationToken cancellationToken)
    {
        if (customMetadataRepository is null || changes.Count == 0)
        {
            return false;
        }

        foreach (var change in changes)
        {
            if (change.Value is null)
            {
                await customMetadataRepository.DeleteValueAsync(bookId, change.FieldId, cancellationToken);
                continue;
            }

            await customMetadataRepository.SetValueAsync(
                change.Value with { BookId = bookId, UpdatedUtc = DateTimeOffset.UtcNow },
                cancellationToken);
        }

        return true;
    }

    private async Task ApplyMetadataValueEditAsync(
        MetadataFilterKind kind,
        string oldValue,
        string? replacementValue,
        bool remove,
        CancellationToken cancellationToken)
    {
        IsCleaningMetadata = true;
        MetadataCleanupStatusText = "Updating metadata...";
        await Task.Yield();
        try
        {
            var changedBooks = new List<Book>();
            foreach (var book in books.Where(book => MetadataValueMatches(book, kind, oldValue)))
            {
                var updated = TryEditMetadataValue(book, kind, oldValue, replacementValue, remove);
                if (!ReferenceEquals(updated, book))
                {
                    changedBooks.Add(updated);
                }
            }

            if (changedBooks.Count == 0)
            {
                return;
            }

            if (TryGetScalarField(kind, out var scalarField) &&
                bookRepository is IBookBulkMetadataRepository bulkRepository)
            {
                var affectedIds = changedBooks.Select(book => book.Id).ToArray();
                var affectedCount = await bulkRepository.UpdateScalarMetadataAsync(
                    affectedIds,
                    scalarField,
                    remove ? null : replacementValue,
                    cancellationToken);
                if (affectedCount == 0)
                {
                    return;
                }

                ApplyPersistedMetadataChanges(changedBooks);
                return;
            }

            if (TryGetListField(kind, out var listField) &&
                bookRepository is IBookBulkMetadataRepository listBulkRepository)
            {
                try
                {
                    var affectedCount = await listBulkRepository.UpdateListMetadataAsync(
                        changedBooks,
                        listField,
                        cancellationToken);
                    if (affectedCount == 0)
                    {
                        return;
                    }

                    ApplyPersistedMetadataChanges(changedBooks);
                    return;
                }
                catch (BookConflictException)
                {
                    // Fall back to per-book updates so one conflicting cleanup does not block the safe changes.
                }
            }

            var persistedBooks = new List<Book>(changedBooks.Count);
            foreach (var changedBook in changedBooks)
            {
                try
                {
                    var fullBook = await bookRepository.GetAsync(changedBook.Id, cancellationToken);
                    if (fullBook is null)
                    {
                        continue;
                    }

                    var bookToPersist = changedBook with
                    {
                        Metadata = new BookMetadata(
                            changedBook.Metadata.Title,
                            changedBook.Metadata.Authors,
                            changedBook.Metadata.Description,
                            changedBook.Metadata.Language,
                            changedBook.Metadata.Publisher,
                            changedBook.Metadata.PublicationDate,
                            changedBook.Metadata.Tags,
                            changedBook.Metadata.Series,
                            changedBook.Metadata.SeriesNumber,
                            changedBook.Metadata.Isbn,
                            fullBook.Metadata.CoverBytes)
                    };
                    await bookRepository.UpdateAsync(bookToPersist, cancellationToken);
                    persistedBooks.Add(bookToPersist);
                }
                catch (BookConflictException)
                {
                    // Keep the original book unchanged when a bulk cleanup would create a duplicate.
                }
            }

            if (persistedBooks.Count == 0)
            {
                return;
            }

            ApplyPersistedMetadataChanges(persistedBooks);
        }
        finally
        {
            IsCleaningMetadata = false;
        }
    }

    private async Task ApplyCustomMetadataValueEditAsync(
        CustomMetadataFieldDefinition definition,
        string oldValue,
        string? replacementValue,
        bool remove,
        CancellationToken cancellationToken)
    {
        if (customMetadataRepository is null || !CanCleanupCustomMetadataValues(definition))
        {
            return;
        }

        IsCleaningMetadata = true;
        MetadataCleanupStatusText = "Updating metadata...";
        await Task.Yield();
        try
        {
            var affectedBookIds = customMetadataValuesByBookId
                .Where(item =>
                    item.Value.TryGetValue(definition.Id, out var value) &&
                    CustomMetadataValueMatches(definition.Type, value, oldValue))
                .Select(item => item.Key)
                .ToArray();
            if (affectedBookIds.Length == 0)
            {
                return;
            }

            var changedBookIds = await customMetadataRepository.CleanupFilterValueAsync(
                definition.Id,
                oldValue,
                replacementValue,
                remove,
                cancellationToken);
            if (changedBookIds.Count == 0)
            {
                return;
            }

            await RefreshCustomMetadataDefinitionsAsync(cancellationToken);
            await RefreshCustomMetadataValuesForBooksAsync(changedBookIds, cancellationToken);
            if (SelectedBook is { } selectedBook &&
                changedBookIds.Contains(selectedBook.Id))
            {
                await Details.LoadCustomMetadataValuesAsync(selectedBook.Id, cancellationToken);
            }

            RefreshFacetFilters();
            ApplyFilter();
        }
        finally
        {
            IsCleaningMetadata = false;
        }
    }

    private async Task NormalizeLanguageMetadataAsync(CancellationToken cancellationToken)
    {
        var changedBooks = books
            .Select(book => (Original: book, NormalizedLanguage: NormalizeStoredLanguageCode(book.Metadata.Language)))
            .Where(change => change.NormalizedLanguage is not null)
            .Select(change => change.Original with
            {
                Metadata = CopyMetadata(
                    change.Original.Metadata,
                    change.Original.Metadata.Authors,
                    change.Original.Metadata.Tags,
                    change.Original.Metadata.Series,
                    change.NormalizedLanguage,
                    change.Original.Metadata.SeriesNumber)
            })
            .ToArray();

        if (changedBooks.Length == 0)
        {
            return;
        }

        if (!await userInteraction.ConfirmLanguageNormalizationAsync(changedBooks.Length, cancellationToken))
        {
            return;
        }

        IsCleaningMetadata = true;
        MetadataCleanupStatusText = "Updating metadata...";
        await Task.Yield();
        try
        {
            var persistedBooks = new List<Book>(changedBooks.Length);
            if (bookRepository is IBookBulkMetadataRepository bulkRepository)
            {
                foreach (var group in changedBooks.GroupBy(book => book.Metadata.Language, StringComparer.OrdinalIgnoreCase))
                {
                    var groupBooks = group.ToArray();
                    var affectedCount = await bulkRepository.UpdateScalarMetadataAsync(
                        groupBooks.Select(book => book.Id).ToArray(),
                        BookScalarMetadataField.Language,
                        group.Key,
                        cancellationToken);
                    if (affectedCount > 0)
                    {
                        persistedBooks.AddRange(groupBooks);
                    }
                }
            }
            else
            {
                foreach (var changedBook in changedBooks)
                {
                    try
                    {
                        await bookRepository.UpdateAsync(changedBook, cancellationToken);
                        persistedBooks.Add(changedBook);
                    }
                    catch (BookConflictException)
                    {
                        // Keep the original book unchanged when cleanup would create a duplicate.
                    }
                }
            }

            if (persistedBooks.Count > 0)
            {
                ApplyPersistedMetadataChanges(persistedBooks);
            }
        }
        finally
        {
            IsCleaningMetadata = false;
        }
    }

    private void ApplyPersistedMetadataChanges(
        IReadOnlyList<Book> persistedBooks,
        bool refreshDisplay = true)
    {
        var persistedById = persistedBooks.ToDictionary(book => book.Id);
        books = books
            .Select(book => persistedById.GetValueOrDefault(book.Id) ?? book)
            .ToList();
        if (SelectedBook is { } selected &&
            persistedById.GetValueOrDefault(selected.Id) is { } selectedChangedBook)
        {
            Details.Load(selectedChangedBook, CurrentLibraryPath);
            _ = Details.LoadCustomMetadataValuesAsync(selectedChangedBook.Id, CancellationToken.None);
        }

        if (!refreshDisplay)
        {
            return;
        }

        RefreshFacetFilters();
        ApplyFilter();
    }

    private static bool TryGetScalarField(
        MetadataFilterKind kind,
        out BookScalarMetadataField field)
    {
        field = kind switch
        {
            MetadataFilterKind.Series => BookScalarMetadataField.Series,
            MetadataFilterKind.Language => BookScalarMetadataField.Language,
            _ => default
        };
        return kind is MetadataFilterKind.Series or MetadataFilterKind.Language;
    }

    private static bool TryGetListField(
        MetadataFilterKind kind,
        out BookListMetadataField field)
    {
        field = kind switch
        {
            MetadataFilterKind.Author => BookListMetadataField.Authors,
            MetadataFilterKind.Tag => BookListMetadataField.Tags,
            _ => default
        };
        return kind is MetadataFilterKind.Author or MetadataFilterKind.Tag;
    }

    private static bool MetadataValueMatches(
        Book book,
        MetadataFilterKind kind,
        string oldValue)
    {
        var metadata = book.Metadata;
        return kind switch
        {
            MetadataFilterKind.Author => metadata.Authors.Any(value =>
                string.Equals(value, oldValue, StringComparison.OrdinalIgnoreCase)),
            MetadataFilterKind.Tag => (metadata.Tags ?? []).Any(value =>
                string.Equals(value, oldValue, StringComparison.OrdinalIgnoreCase)),
            MetadataFilterKind.Series => ScalarValueMatches(metadata.Series, oldValue),
            MetadataFilterKind.Language => ScalarValueMatches(metadata.Language, oldValue, LanguageDisplayService.FilterKey),
            _ => false
        };
    }

    private static bool ScalarValueMatches(
        string? source,
        string oldValue,
        Func<string?, string?>? comparisonKeySelector = null)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        var sourceKey = comparisonKeySelector?.Invoke(source) ?? source.Trim();
        var oldKey = comparisonKeySelector?.Invoke(oldValue) ?? oldValue.Trim();
        return string.Equals(sourceKey, oldKey, StringComparison.OrdinalIgnoreCase);
    }

    private static Book TryEditMetadataValue(
        Book book,
        MetadataFilterKind kind,
        string oldValue,
        string? replacementValue,
        bool remove)
    {
        var metadata = book.Metadata;
        return kind switch
        {
            MetadataFilterKind.Author => ReplaceListValue(
                    metadata.Authors,
                    oldValue,
                    replacementValue,
                    remove,
                    out var authors)
                ? book with { Metadata = CopyMetadata(metadata, authors, metadata.Tags, metadata.Series, metadata.Language, metadata.SeriesNumber) }
                : book,
            MetadataFilterKind.Tag => ReplaceListValue(
                    metadata.Tags ?? [],
                    oldValue,
                    replacementValue,
                    remove,
                    out var tags)
                ? book with { Metadata = CopyMetadata(metadata, metadata.Authors, tags.Count == 0 ? null : tags, metadata.Series, metadata.Language, metadata.SeriesNumber) }
                : book,
            MetadataFilterKind.Series => ReplaceScalarValue(
                    metadata.Series,
                    oldValue,
                    replacementValue,
                    remove,
                    out var series)
                ? book with { Metadata = CopyMetadata(metadata, metadata.Authors, metadata.Tags, series, metadata.Language, series is null ? null : metadata.SeriesNumber) }
                : book,
            MetadataFilterKind.Language => ReplaceScalarValue(
                    metadata.Language,
                    oldValue,
                    replacementValue,
                    remove,
                    out var language,
                    LanguageDisplayService.FilterKey)
                ? book with { Metadata = CopyMetadata(metadata, metadata.Authors, metadata.Tags, metadata.Series, language, metadata.SeriesNumber) }
                : book,
            _ => book
        };
    }

    private static bool ReplaceListValue(
        IReadOnlyList<string> source,
        string oldValue,
        string? replacementValue,
        bool remove,
        out IReadOnlyList<string> updated)
    {
        var changed = false;
        var values = new List<string>();
        foreach (var value in source)
        {
            if (string.Equals(value, oldValue, StringComparison.OrdinalIgnoreCase))
            {
                changed = true;
                if (!remove && !string.IsNullOrWhiteSpace(replacementValue))
                {
                    values.Add(replacementValue.Trim());
                }
            }
            else
            {
                values.Add(value);
            }
        }

        updated = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return changed;
    }

    private static bool ReplaceScalarValue(
        string? source,
        string oldValue,
        string? replacementValue,
        bool remove,
        out string? updated,
        Func<string?, string?>? comparisonKeySelector = null)
    {
        updated = source;
        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        var sourceKey = comparisonKeySelector?.Invoke(source) ?? source.Trim();
        var oldKey = comparisonKeySelector?.Invoke(oldValue) ?? oldValue.Trim();
        if (!string.Equals(sourceKey, oldKey, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        updated = remove ? null : replacementValue?.Trim();
        return true;
    }

    private static BookMetadata CopyMetadata(
        BookMetadata metadata,
        IReadOnlyList<string> authors,
        IReadOnlyList<string>? tags,
        string? series,
        string? language,
        decimal? seriesNumber) =>
        CopyMetadata(metadata, metadata.Title, authors, tags, series, language, seriesNumber);

    private static BookMetadata CopyMetadata(
        BookMetadata metadata,
        string title,
        IReadOnlyList<string> authors,
        IReadOnlyList<string>? tags,
        string? series,
        string? language,
        decimal? seriesNumber) =>
        new(
            title,
            authors,
            metadata.Description,
            language,
            metadata.Publisher,
            metadata.PublicationDate,
            tags,
            series,
            seriesNumber,
            metadata.Isbn,
            metadata.CoverBytes);

    private static IReadOnlyList<string> SplitRequiredList(string? value) =>
        SplitList(value, distinct: true);

    private static IReadOnlyList<string>? SplitNullableList(string? value)
    {
        var values = SplitList(value, distinct: true);
        return values.Count == 0 ? null : values;
    }

    private static IReadOnlyList<string>? ApplyTagAction(
        IReadOnlyList<string>? currentTags,
        MetadataMultiEditTagAction action,
        string? tagsText)
    {
        var editedTags = SplitList(tagsText, distinct: true);
        return action switch
        {
            MetadataMultiEditTagAction.Add => AddTags(currentTags, editedTags),
            MetadataMultiEditTagAction.Remove => RemoveTags(currentTags, editedTags),
            _ => editedTags.Count == 0 ? null : editedTags
        };
    }

    private static IReadOnlyList<string>? AddTags(
        IReadOnlyList<string>? currentTags,
        IReadOnlyList<string> tagsToAdd)
    {
        var tags = (currentTags ?? []).ToList();
        foreach (var tag in tagsToAdd)
        {
            if (!tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                tags.Add(tag);
            }
        }

        return tags.Count == 0 ? null : tags;
    }

    private static IReadOnlyList<string>? RemoveTags(
        IReadOnlyList<string>? currentTags,
        IReadOnlyList<string> tagsToRemove)
    {
        if (currentTags is null || currentTags.Count == 0)
        {
            return null;
        }

        var removeSet = tagsToRemove.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tags = currentTags.Where(tag => !removeSet.Contains(tag)).ToArray();
        return tags.Length == 0 ? null : tags;
    }

    private static IReadOnlyList<string> SplitList(string? value, bool distinct = false) =>
        CustomMetadataValueParser.SplitList(value, distinct);

    private static string? NormalizeBlank(string? value)
        => CustomMetadataValueParser.NormalizeBlank(value);

    private static bool NullableSequenceEqual<T>(
        IReadOnlyList<T>? first,
        IReadOnlyList<T>? second) =>
        first is null ? second is null : second is not null && first.SequenceEqual(second);

    private static CustomMetadataValue CreateCustomMetadataValue(
        Guid bookId,
        MetadataMultiEditCustomFieldResult value) =>
        CustomMetadataValueParser.Create(bookId, value.FieldId, value.Name, value.Type, value.ValueText);

    private static string? NormalizeStoredLanguageCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        var normalized = LanguageDisplayService.FilterKey(trimmed);
        return string.IsNullOrWhiteSpace(normalized) ||
            string.Equals(trimmed, normalized, StringComparison.OrdinalIgnoreCase)
            ? null
            : normalized;
    }

    private static IEnumerable<BookRowViewModel> ApplySort(
        IEnumerable<BookRowViewModel> rows,
        LibrarySortOption sortOption,
        AuthorSortStrategy authorSortStrategy)
    {
        return sortOption switch
        {
            LibrarySortOption.Title => rows
                .OrderBy(row => row.Title, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(row => row.Authors, StringComparer.CurrentCultureIgnoreCase),
            LibrarySortOption.Author => rows
                .OrderBy(row => AuthorSortKeyBuilder.BuildSortKey(row.Authors, authorSortStrategy), StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(row => row.Title, StringComparer.CurrentCultureIgnoreCase),
            LibrarySortOption.EReader => rows
                .OrderBy(row => row.EReader, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(row => row.Title, StringComparer.CurrentCultureIgnoreCase),
            LibrarySortOption.Category => rows
                .OrderBy(row => row.Book.Metadata.Tags?.FirstOrDefault() ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(row => row.Title, StringComparer.CurrentCultureIgnoreCase),
            _ => rows
        };
    }

    partial void OnCurrentLibraryPathChanged(string? value)
    {
        OnPropertyChanged(nameof(HasActiveLibrary));
        showDuplicateExclusionsCommand?.NotifyCanExecuteChanged();
    }

    public bool ApplyDefaultViewPreference(string? defaultView)
    {
        if (!string.IsNullOrWhiteSpace(defaultView))
        {
            var definition = ViewDefinitions.FirstOrDefault(
                view => view.Id.Equals(defaultView.Trim(), StringComparison.OrdinalIgnoreCase));
            if (definition is not null)
            {
                SelectedViewDefinitionId = definition.Id;
                return true;
            }
        }

        if (!Enum.TryParse<LibraryView>(defaultView, ignoreCase: true, out var parsedView) ||
            !Enum.IsDefined(parsedView))
        {
            return false;
        }

        SelectedView = parsedView;
        return true;
    }

    private async Task ApplyDefaultViewAsync(CancellationToken cancellationToken)
    {
        if (hasAppliedDefaultView || settingsStore is null)
        {
            return;
        }

        hasAppliedDefaultView = true;
        var settings = await settingsStore.LoadAsync(cancellationToken);
        authorSortStrategy = settings.AuthorSortStrategy;
        LoadViewLayoutSettings(settings);
        ApplyDefaultViewPreference(settings.DefaultView);
        RefreshActiveGroupOptions();
        RefreshActiveColumnOptions();
        ApplySelectedViewSortOption();
    }

    private void RefreshLibraryDisplay()
    {
        CurrentLibraryName = currentLibrary?.Current?.Name;
        CurrentLibraryPath = currentLibrary?.Current?.DirectoryPath;
    }

    private bool EnsureActiveLibraryStillExists(string noActiveLibraryMessage)
    {
        if (!HasActiveLibrary)
        {
            EmptyStateMessage = noActiveLibraryMessage;
            return false;
        }

        if (CurrentLibraryPath is not { Length: > 0 } currentPath || Directory.Exists(currentPath))
        {
            return true;
        }

        currentLibrary?.Clear();
        books = [];
        VisibleBooks.ReplaceAll([]);
        GroupedLibraryNodes.ReplaceAll([]);
        AuthorFilters.Clear();
        CategoryFilters.Clear();
        SeriesFilters.Clear();
        StatusFilters.Clear();
        EReaderFilters.Clear();
        LanguageFilters.Clear();
        FormatFilters.Clear();
        CustomMetadataFilterGroups.Clear();
        OnPropertyChanged(nameof(HasCustomMetadataFilterGroups));
        Details.Clear();
        RefreshLibraryDisplay();
        EmptyStateMessage = MissingActiveLibraryMessage;
        OnPropertyChanged(nameof(VisibleBookCount));
        OnPropertyChanged(nameof(HasActiveLibrary));
        return false;
    }

    private async void OnDetailsBookSaved(object? sender, Book savedBook)
    {
        try
        {
            await HandleDetailsBookSavedAsync(savedBook);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
        }
    }

    private async Task HandleDetailsBookSavedAsync(Book savedBook)
    {
        var mutableBooks = books.ToList();
        var index = mutableBooks.FindIndex(book => book.Id == savedBook.Id);
        if (index >= 0)
        {
            mutableBooks[index] = savedBook;
        }
        else
        {
            mutableBooks.Add(savedBook);
        }

        books = mutableBooks;
        await RefreshCustomMetadataValuesForBookAsync(savedBook.Id, CancellationToken.None);
        RefreshFacetFilters();
        ApplyFilter();
    }

    private void OnDetailsBookDeleted(object? sender, Guid bookId)
    {
        books = books.Where(book => book.Id != bookId).ToList();
        RefreshFacetFilters();
        ApplyFilter();
    }

    private static Task OnImportProgressAsync(ImportProgress progress) => Task.CompletedTask;

    private async void OnImportAgentCompleted(object? sender, ImportBatchResult result)
    {
        LastImportResult = CreateImportResultViewModel(result);
        OnPropertyChanged(nameof(HasActiveImport));
        await RefreshAsync(CancellationToken.None);
    }

    private async Task ShowImportDetailsAsync(CancellationToken cancellationToken)
    {
        var result = importAgent?.Job.LatestResult;
        if (result is null)
        {
            return;
        }

        LastImportResult = CreateImportResultViewModel(result);
        await userInteraction.ShowImportResultAsync(LastImportResult, cancellationToken);
    }

    private async Task ShowImportHistoryAsync(CancellationToken cancellationToken)
    {
        if (!EnsureActiveLibraryStillExists("Create or open a library to get started.") || importRepository is null)
        {
            return;
        }

        var summaries = await importRepository.ListRecentAsync(50, cancellationToken);
        var history = new ImportHistoryViewModel(summaries);
        var selectedRunId = await userInteraction.PickImportRunAsync(history, cancellationToken);
        if (selectedRunId is null)
        {
            return;
        }

        var run = await importRepository.GetAsync(selectedRunId.Value, cancellationToken);
        if (run is null)
        {
            return;
        }

        LastImportResult = CreateImportResultViewModel(run);
        await userInteraction.ShowImportResultAsync(LastImportResult, cancellationToken);
    }

    private ImportResultViewModel CreateImportResultViewModel(ImportBatchResult result) =>
        new(result, RetryFailedImportsAsync, LinkImportSuggestionAsync, LocalizeImportPhaseName);

    private ImportResultViewModel CreateImportResultViewModel(ImportRunResult result) =>
        new(result, RetryFailedImportsAsync, LinkImportSuggestionAsync, LocalizeImportPhaseName);

    private string LocalizeImportPhaseName(string phaseName) =>
        phaseName switch
        {
            "local" => localize("ImportPhaseLocal"),
            "size" => localize("ImportPhaseSize"),
            "hash" => localize("ImportPhaseHash"),
            "meta" => localize("ImportPhaseMetadata"),
            "dup" => localize("ImportPhaseDuplicate"),
            "copy" => localize("ImportPhaseCopy"),
            "db" => localize("ImportPhaseDatabase"),
            "cleanup" => localize("ImportPhaseCleanup"),
            _ => phaseName
        };

    private Task RetryFailedImportsAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken) =>
        ImportFilesAsync(paths, cancellationToken, ImportRunContext.FileImport);

    private void ReportPerformance(LibraryViewPerformanceTracker tracker, int visibleCount)
    {
        performanceReporter?.Report(new LibraryPerformanceSnapshot(
            tracker.Operation,
            tracker.Elapsed,
            books.Count,
            visibleCount,
            GroupedLibraryNodes.Count,
            GetActiveGroupOptions(),
            SelectedSortOption,
            tracker.Phases));
    }

    private async Task LinkImportSuggestionAsync(
        Guid sourceBookId,
        Guid targetBookId,
        CancellationToken cancellationToken)
    {
        await bookRepository.AttachFilesToBookAsync(sourceBookId, targetBookId, cancellationToken);
        await RefreshAsync(cancellationToken);
    }

    private async Task ShowDuplicateCandidatesAsync(CancellationToken cancellationToken)
    {
        if (!EnsureActiveLibraryStillExists("Create or open a library to get started."))
        {
            return;
        }

        var excludedPairs = duplicateExclusionRepository is null
            ? new HashSet<DuplicateExclusionPair>()
            : await duplicateExclusionRepository.ListDuplicateExclusionsAsync(cancellationToken);
        var result = duplicateCandidateService.FindCandidates(books, excludedPairs);
        var settings = settingsStore is null
            ? null
            : await settingsStore.LoadAsync(cancellationToken);
        var candidates = new DuplicateCandidatesViewModel(
            result,
            CurrentLibraryPath,
            DeleteDuplicateCandidateAsync,
            MergeDuplicateCandidateAsync,
            IgnoreDuplicateCandidatesAsync,
            excludedPairs);
        candidates.ExactMatchesOnly = settings?.DuplicateExactMatchesOnly ?? true;
        candidates.MergeDefaults = settings?.DuplicateMergeDefaults ?? new DuplicateMergeDefaultSettings();
        await userInteraction.ShowDuplicateCandidatesAsync(
            candidates,
            cancellationToken);
        if (candidates.HasChanges)
        {
            await RefreshAsync(cancellationToken);
        }
    }

    private async Task<bool> DeleteDuplicateCandidateAsync(
        DuplicateCandidateRowViewModel row,
        CancellationToken cancellationToken)
    {
        if (bookService is null)
        {
            return false;
        }

        if (!await userInteraction.ConfirmDeleteAsync(row.Title, cancellationToken))
        {
            return false;
        }

        var result = await bookService.DeleteAsync(row.Id, cancellationToken);
        return result.Status == BookDeleteStatus.Deleted;
    }

    private async Task<bool> MergeDuplicateCandidateAsync(
        DuplicateCandidateRowViewModel sourceRow,
        DuplicateCandidateRowViewModel targetRow,
        IReadOnlyList<DuplicateMergeFieldSelection> selections,
        CancellationToken cancellationToken)
    {
        try
        {
            await duplicateMergeService.MergeAsync(sourceRow.Id, targetRow.Id, selections, cancellationToken);
            return true;
        }
        catch (KeyNotFoundException exception)
        {
            await RefreshAsync(cancellationToken);
            throw new InvalidOperationException("The duplicate list is outdated. Open the duplicate overview again.", exception);
        }
    }

    private async Task ShowDuplicateExclusionsAsync(CancellationToken cancellationToken)
    {
        if (duplicateExclusionRepository is null ||
            !EnsureActiveLibraryStillExists("Create or open a library to get started."))
        {
            return;
        }

        var exclusions = new DuplicateExclusionsViewModel(duplicateExclusionRepository);
        await exclusions.LoadAsync(cancellationToken);
        await userInteraction.ShowDuplicateExclusionsAsync(exclusions, cancellationToken);
    }

    private async Task ShowMetadataQualityDashboardAsync(CancellationToken cancellationToken)
    {
        if (!EnsureActiveLibraryStillExists("Create or open a library to get started."))
        {
            return;
        }

        var selectedBookId = await userInteraction.ShowMetadataQualityDashboardAsync(
            new MetadataQualityDashboardViewModel(books, localize),
            cancellationToken);
        if (selectedBookId is { } bookId)
        {
            RevealBookInLibrary(bookId);
        }
    }

    private void RevealBookInLibrary(Guid bookId)
    {
        var book = books.FirstOrDefault(candidate => candidate.Id == bookId);
        if (book is null)
        {
            return;
        }

        if (VisibleBooks.All(row => row.Id != bookId))
        {
            isSuppressingFilterRefresh = true;
            try
            {
                if (searchService.Filter(
                        [book],
                        SearchText,
                        candidate => GetCustomMetadataValues(candidate.Id).Values).Count == 0)
                {
                    SearchText = string.Empty;
                }

                if (ApplyFacetFilters([book]).Count == 0)
                {
                    ClearSelectedFacetFilters();
                }
            }
            finally
            {
                isSuppressingFilterRefresh = false;
            }

            ApplyFilter();
        }

        var selectedRow = VisibleBooks.FirstOrDefault(row => row.Id == bookId);
        if (selectedRow is not null)
        {
            SelectedBook = selectedRow;
            SetSelectedBooks([selectedRow]);
            ExpandFirstGroupPathToBook(bookId);
            BookRevealRequest = new LibraryBookRevealRequest(bookId, ++bookRevealSequence);
        }
    }

    private void ExpandFirstGroupPathToBook(Guid bookId)
    {
        foreach (var group in GroupedLibraryNodes)
        {
            if (group.TryExpandPathToBook(bookId))
            {
                return;
            }
        }
    }

    private void ClearSelectedFacetFilters()
    {
        foreach (var filter in StandardFacetFilterCollections()
                     .SelectMany(filters => filters)
                     .Concat(CustomMetadataFilterGroups.SelectMany(group => group.Filters))
                     .Where(filter => filter.IsSelected))
        {
            filter.IsSelected = false;
        }
    }

    private IEnumerable<ObservableCollection<FacetFilterViewModel>> StandardFacetFilterCollections()
    {
        yield return AuthorFilters;
        yield return CategoryFilters;
        yield return SeriesFilters;
        yield return StatusFilters;
        yield return EReaderFilters;
        yield return LanguageFilters;
        yield return FormatFilters;
    }

    private async Task IgnoreDuplicateCandidatesAsync(
        IReadOnlyCollection<DuplicateExclusionPair> pairs,
        CancellationToken cancellationToken)
    {
        if (duplicateExclusionRepository is null)
        {
            return;
        }

        await duplicateExclusionRepository.AddDuplicateExclusionsAsync(pairs, cancellationToken);
    }

    private enum MetadataFilterKind
    {
        Author,
        Series,
        Tag,
        Language
    }

    private sealed class LibraryViewPerformanceTracker
    {
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        private readonly Dictionary<string, TimeSpan> phases = new(StringComparer.Ordinal);

        public LibraryViewPerformanceTracker(string operation) => Operation = operation;

        public string Operation { get; }

        public TimeSpan Elapsed => stopwatch.Elapsed;

        public IReadOnlyDictionary<string, TimeSpan> Phases => phases;

        public T Measure<T>(string phase, Func<T> action)
        {
            var phaseStopwatch = Stopwatch.StartNew();
            try
            {
                return action();
            }
            finally
            {
                RecordPhase(phase, phaseStopwatch.Elapsed);
            }
        }

        public void Measure(string phase, Action action)
        {
            var phaseStopwatch = Stopwatch.StartNew();
            try
            {
                action();
            }
            finally
            {
                RecordPhase(phase, phaseStopwatch.Elapsed);
            }
        }

        public async Task MeasureAsync(string phase, Func<Task> action)
        {
            var phaseStopwatch = Stopwatch.StartNew();
            try
            {
                await action();
            }
            finally
            {
                RecordPhase(phase, phaseStopwatch.Elapsed);
            }
        }

        private void RecordPhase(string phase, TimeSpan elapsed)
        {
            phases[phase] = phases.TryGetValue(phase, out var existing)
                ? existing + elapsed
                : elapsed;
        }
    }
}

public sealed record LibraryBookRevealRequest(Guid BookId, int Sequence);

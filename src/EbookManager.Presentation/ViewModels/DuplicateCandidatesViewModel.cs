using EbookManager.Application.Books;
using EbookManager.Domain.Books;
using EbookManager.Domain.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class DuplicateCandidatesViewModel : ObservableObject
{
    private readonly DuplicateCandidateService duplicateCandidateService = new();
    private readonly string? libraryPath;
    private readonly Func<DuplicateCandidateRowViewModel, CancellationToken, Task<bool>>? deleteCandidateAsync;
    private readonly Func<DuplicateCandidateRowViewModel, DuplicateCandidateRowViewModel, IReadOnlyList<DuplicateMergeFieldSelection>, CancellationToken, Task<bool>>? mergeCandidateAsync;
    private readonly AsyncRelayCommand deleteSelectedCandidatesCommand;
    private IReadOnlyList<Book> books;
    private IReadOnlyList<DuplicateCandidateGroup> allGroups;
    private DuplicateMergeDefaultSettings mergeDefaults = new();
    private bool exactMatchesOnly = true;

    public DuplicateCandidatesViewModel(
        DuplicateCandidateResult result,
        string? libraryPath = null,
        Func<DuplicateCandidateRowViewModel, CancellationToken, Task<bool>>? deleteCandidateAsync = null,
        Func<DuplicateCandidateRowViewModel, DuplicateCandidateRowViewModel, IReadOnlyList<DuplicateMergeFieldSelection>, CancellationToken, Task<bool>>? mergeCandidateAsync = null)
    {
        this.libraryPath = libraryPath;
        this.deleteCandidateAsync = deleteCandidateAsync;
        this.mergeCandidateAsync = mergeCandidateAsync;
        books = result.Groups
            .SelectMany(group => group.Books)
            .DistinctBy(book => book.Id)
            .ToList()
            .AsReadOnly();
        allGroups = result.Groups;
        deleteSelectedCandidatesCommand = new AsyncRelayCommand(
            DeleteSelectedCandidatesAsync,
            () => Rows.Any(row => row.IsSelected));
        ApplyVisibleGroups();
    }

    public ObservableCollection<DuplicateCandidateGroupViewModel> Groups { get; } = [];
    public ObservableCollection<DuplicateCandidateRowViewModel> Rows { get; } = [];
    public int GroupCount => Groups.Count;
    public int BookCount => Groups.Sum(group => group.Books.Count);
    public bool HasGroups => Groups.Count > 0;
    public string SummaryText => $"{GroupCount} groups, {BookCount} books";
    public bool HasChanges { get; private set; }
    public bool HasMergeSuccessMessage => !string.IsNullOrWhiteSpace(MergeSuccessBookTitle);
    public string MergeSuccessBookTitle { get; private set; } = string.Empty;
    public IAsyncRelayCommand DeleteSelectedCandidatesCommand => deleteSelectedCandidatesCommand;
    public bool ExactMatchesOnly
    {
        get => exactMatchesOnly;
        set
        {
            if (SetProperty(ref exactMatchesOnly, value))
            {
                ApplyVisibleGroups();
            }
        }
    }

    public DuplicateMergeDefaultSettings MergeDefaults
    {
        get => mergeDefaults;
        set => mergeDefaults = value ?? new DuplicateMergeDefaultSettings();
    }

    public void SetSelectedRows(IEnumerable<DuplicateCandidateRowViewModel> selectedRows)
    {
        var selectedIds = selectedRows.Select(row => row.Id).ToHashSet();
        foreach (var row in Rows)
        {
            row.IsSelected = selectedIds.Contains(row.Id);
        }

        deleteSelectedCandidatesCommand.NotifyCanExecuteChanged();
    }

    public async Task DeleteCandidateAsync(
        DuplicateCandidateRowViewModel row,
        CancellationToken cancellationToken)
    {
        if (deleteCandidateAsync is null)
        {
            return;
        }

        var deleted = await deleteCandidateAsync(row, cancellationToken);
        if (!deleted)
        {
            return;
        }

        books = books
            .Where(book => book.Id != row.Id)
            .ToList()
            .AsReadOnly();
        HasChanges = true;
        ClearMergeSuccessMessage();
        OnPropertyChanged(nameof(HasChanges));
        ApplyResult(duplicateCandidateService.FindCandidates(books));
    }

    public async Task MergeCandidateAsync(
        DuplicateCandidateRowViewModel sourceRow,
        CancellationToken cancellationToken)
    {
        var preview = CreateMergePreview(sourceRow);
        if (preview is null)
        {
            return;
        }

        await MergeCandidateAsync(preview, cancellationToken);
    }

    public async Task MergeCandidateAsync(
        DuplicateMergePreviewViewModel preview,
        CancellationToken cancellationToken)
    {
        if (mergeCandidateAsync is null)
        {
            return;
        }

        var merged = await mergeCandidateAsync(
            preview.Source,
            preview.Target,
            preview.BuildSelections(),
            cancellationToken);
        if (!merged)
        {
            return;
        }

        books = books
            .Where(book => book.Id != preview.Source.Id)
            .ToList()
            .AsReadOnly();
        HasChanges = true;
        SetMergeSuccessMessage(preview.Target.Title);
        OnPropertyChanged(nameof(HasChanges));
        ApplyResult(duplicateCandidateService.FindCandidates(books));
    }

    public void ClearMergeSuccessMessage()
    {
        if (string.IsNullOrWhiteSpace(MergeSuccessBookTitle))
        {
            return;
        }

        MergeSuccessBookTitle = string.Empty;
        OnPropertyChanged(nameof(MergeSuccessBookTitle));
        OnPropertyChanged(nameof(HasMergeSuccessMessage));
    }

    public DuplicateMergePreviewViewModel? CreateMergePreview(DuplicateCandidateRowViewModel clickedRow)
    {
        var mergePair = SelectMergePair(clickedRow);
        return mergePair is null
            ? null
            : new DuplicateMergePreviewViewModel(mergePair.Value.Source, mergePair.Value.Target, MergeDefaults);
    }

    private async Task DeleteSelectedCandidatesAsync(CancellationToken cancellationToken)
    {
        var selectedRows = Rows
            .Where(row => row.IsSelected)
            .ToList();
        if (selectedRows.Count == 0)
        {
            return;
        }

        foreach (var row in selectedRows)
        {
            if (deleteCandidateAsync is null)
            {
                return;
            }

            var deleted = await deleteCandidateAsync(row, cancellationToken);
            if (!deleted)
            {
                continue;
            }

            books = books
                .Where(book => book.Id != row.Id)
                .ToList()
                .AsReadOnly();
            HasChanges = true;
            ClearMergeSuccessMessage();
        }

        OnPropertyChanged(nameof(HasChanges));
        ApplyResult(duplicateCandidateService.FindCandidates(books));
    }

    private void SetMergeSuccessMessage(string bookTitle)
    {
        MergeSuccessBookTitle = bookTitle;
        OnPropertyChanged(nameof(MergeSuccessBookTitle));
        OnPropertyChanged(nameof(HasMergeSuccessMessage));
    }

    public static string BuildGroupTitle(DuplicateCandidateGroup group) =>
        string.IsNullOrWhiteSpace(group.AuthorSummary)
            ? group.DisplayTitle
            : $"{group.DisplayTitle} - {group.AuthorSummary}";

    private void ApplyResult(DuplicateCandidateResult result)
    {
        allGroups = result.Groups;
        ApplyVisibleGroups();
    }

    private void ApplyVisibleGroups()
    {
        Groups.Clear();
        Rows.Clear();
        foreach (var group in FilterGroups(allGroups))
        {
            Groups.Add(new DuplicateCandidateGroupViewModel(group));
            foreach (var book in group.Books)
            {
                var row = new DuplicateCandidateRowViewModel(
                    group.MatchKey,
                    BuildGroupTitle(group),
                    group.MatchKind,
                    book,
                    libraryPath);
                row.PropertyChanged += OnRowPropertyChanged;
                Rows.Add(row);
            }
        }

        OnPropertyChanged(nameof(GroupCount));
        OnPropertyChanged(nameof(BookCount));
        OnPropertyChanged(nameof(HasGroups));
        OnPropertyChanged(nameof(SummaryText));
        deleteSelectedCandidatesCommand.NotifyCanExecuteChanged();
    }

    private IEnumerable<DuplicateCandidateGroup> FilterGroups(IReadOnlyList<DuplicateCandidateGroup> candidateGroups) =>
        exactMatchesOnly
            ? candidateGroups.Where(group => group.MatchKind == DuplicateCandidateMatchKind.AuthorOverlap)
            : candidateGroups;

    private (DuplicateCandidateRowViewModel Source, DuplicateCandidateRowViewModel Target)? SelectMergePair(
        DuplicateCandidateRowViewModel clickedRow)
    {
        var groupRows = Rows
            .Where(row => row.MatchKey == clickedRow.MatchKey)
            .OrderByDescending(row => row.MetadataScore)
            .ThenBy(row => row.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        if (groupRows.Count < 2)
        {
            return null;
        }

        var target = groupRows[0];
        var source = clickedRow.Id == target.Id
            ? groupRows[1]
            : clickedRow;
        return (source, target);
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DuplicateCandidateRowViewModel.IsSelected))
        {
            deleteSelectedCandidatesCommand.NotifyCanExecuteChanged();
        }
    }
}

public sealed partial class DuplicateMergePreviewViewModel : ObservableObject
{
    private readonly DuplicateMergeDefaultSettings mergeDefaults;
    private readonly RelayCommand swapDirectionCommand;

    public DuplicateMergePreviewViewModel(
        DuplicateCandidateRowViewModel source,
        DuplicateCandidateRowViewModel target,
        DuplicateMergeDefaultSettings? mergeDefaults = null)
    {
        Source = source;
        Target = target;
        this.mergeDefaults = mergeDefaults ?? new DuplicateMergeDefaultSettings();
        swapDirectionCommand = new RelayCommand(SwapDirection);
        RefreshRows();
    }

    public DuplicateCandidateRowViewModel Source { get; private set; }
    public DuplicateCandidateRowViewModel Target { get; private set; }
    public ObservableCollection<DuplicateMergeFieldRowViewModel> Rows { get; } = [];
    public IRelayCommand SwapDirectionCommand => swapDirectionCommand;

    private void SwapDirection()
    {
        (Source, Target) = (Target, Source);
        OnPropertyChanged(nameof(Source));
        OnPropertyChanged(nameof(Target));
        RefreshRows();
    }

    private void RefreshRows()
    {
        Rows.Clear();
        Rows.Add(DuplicateMergeFieldRowViewModel.CreateCover(Target.CoverPath, Source.CoverPath, mergeDefaults.Cover));
        Rows.Add(new DuplicateMergeFieldRowViewModel(DuplicateMergeMetadataField.Title, "Title", Target.Title, Source.Title, defaultAction: mergeDefaults.Title));
        Rows.Add(new DuplicateMergeFieldRowViewModel(DuplicateMergeMetadataField.Authors, "Authors", Target.Authors, Source.Authors, defaultAction: mergeDefaults.Authors));
        Rows.Add(new DuplicateMergeFieldRowViewModel(DuplicateMergeMetadataField.Formats, "Formats", Target.FormatText, Source.FormatText));
        Rows.Add(new DuplicateMergeFieldRowViewModel(DuplicateMergeMetadataField.Series, "Series", Target.Series, Source.Series, defaultAction: mergeDefaults.Series));
        Rows.Add(new DuplicateMergeFieldRowViewModel(DuplicateMergeMetadataField.SeriesNumber, "SeriesNumber", Target.SeriesNumber, Source.SeriesNumber, defaultAction: mergeDefaults.SeriesNumber));
        Rows.Add(new DuplicateMergeFieldRowViewModel(DuplicateMergeMetadataField.Language, "Language", Target.Language, Source.Language, defaultAction: mergeDefaults.Language));
        Rows.Add(new DuplicateMergeFieldRowViewModel(DuplicateMergeMetadataField.Publisher, "Publisher", Target.Publisher, Source.Publisher, defaultAction: mergeDefaults.Publisher));
        Rows.Add(new DuplicateMergeFieldRowViewModel(DuplicateMergeMetadataField.PublicationDate, "PublicationDate", Target.PublicationDate, Source.PublicationDate, defaultAction: mergeDefaults.PublicationDate));
        Rows.Add(new DuplicateMergeFieldRowViewModel(DuplicateMergeMetadataField.Isbn, "Isbn", Target.Isbn, Source.Isbn, defaultAction: mergeDefaults.Isbn));
        Rows.Add(new DuplicateMergeFieldRowViewModel(DuplicateMergeMetadataField.Tags, "Tags", Target.Tags, Source.Tags, defaultAction: mergeDefaults.Tags));
        Rows.Add(new DuplicateMergeFieldRowViewModel(DuplicateMergeMetadataField.Description, "Description", Target.Description, Source.Description, isLargeText: true, defaultAction: mergeDefaults.Description));
    }

    public IReadOnlyList<DuplicateMergeFieldSelection> BuildSelections() =>
        Rows
            .Select(row => new DuplicateMergeFieldSelection(row.Field, row.Action switch
            {
                DuplicateMergeFieldAction.Copy => DuplicateMergeAction.Copy,
                DuplicateMergeFieldAction.Merge => DuplicateMergeAction.Merge,
                _ => DuplicateMergeAction.NoAction
            }))
            .ToArray();
}

public sealed partial class DuplicateMergeFieldRowViewModel : ObservableObject
{
    public DuplicateMergeFieldRowViewModel(
        DuplicateMergeMetadataField field,
        string label,
        string targetValue,
        string sourceValue,
        bool isLargeText = false,
        bool canMerge = true,
        bool isCover = false,
        string? targetImagePath = null,
        string? sourceImagePath = null,
        DuplicateMergeDefaultAction defaultAction = DuplicateMergeDefaultAction.NoAction)
    {
        Field = field;
        Label = label;
        TargetValue = targetValue;
        SourceValue = sourceValue;
        IsLargeText = isLargeText;
        CanMerge = canMerge;
        IsCover = isCover;
        TargetImagePath = targetImagePath;
        SourceImagePath = sourceImagePath;
        IsActionEnabled = !ValuesAreEqual(targetValue, sourceValue);
        Action = MapDefaultAction(defaultAction, CanMerge, IsActionEnabled);
        cycleActionCommand = new RelayCommand(CycleAction, () => IsActionEnabled);
    }

    private readonly RelayCommand cycleActionCommand;

    [ObservableProperty]
    private DuplicateMergeFieldAction action;

    public string Label { get; }
    public DuplicateMergeMetadataField Field { get; }
    public string TargetValue { get; }
    public string SourceValue { get; }
    public bool IsLargeText { get; }
    public bool CanMerge { get; }
    public bool IsCover { get; }
    public bool IsActionEnabled { get; }
    public string? TargetImagePath { get; }
    public string? SourceImagePath { get; }
    public IRelayCommand CycleActionCommand => cycleActionCommand;

    public static DuplicateMergeFieldRowViewModel CreateCover(
        string? targetImagePath,
        string? sourceImagePath,
        DuplicateMergeDefaultAction defaultAction = DuplicateMergeDefaultAction.NoAction) =>
        new(
            DuplicateMergeMetadataField.Cover,
            "Cover",
            targetImagePath ?? string.Empty,
            sourceImagePath ?? string.Empty,
            canMerge: false,
            isCover: true,
            targetImagePath: targetImagePath,
            sourceImagePath: sourceImagePath,
            defaultAction: defaultAction);

    private void CycleAction()
    {
        if (!IsActionEnabled)
        {
            return;
        }

        Action = Action switch
        {
            DuplicateMergeFieldAction.NoAction => DuplicateMergeFieldAction.Copy,
            DuplicateMergeFieldAction.Copy when CanMerge => DuplicateMergeFieldAction.Merge,
            _ => DuplicateMergeFieldAction.NoAction
        };
    }

    private static bool ValuesAreEqual(string targetValue, string sourceValue) =>
        string.Equals(
            NormalizeForComparison(targetValue),
            NormalizeForComparison(sourceValue),
            StringComparison.Ordinal);

    private static string NormalizeForComparison(string value) =>
        string.Join(
            ' ',
            value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static DuplicateMergeFieldAction MapDefaultAction(
        DuplicateMergeDefaultAction defaultAction,
        bool canMerge,
        bool isActionEnabled) =>
        isActionEnabled
            ? defaultAction switch
            {
                DuplicateMergeDefaultAction.Copy => DuplicateMergeFieldAction.Copy,
                DuplicateMergeDefaultAction.Merge when canMerge => DuplicateMergeFieldAction.Merge,
                _ => DuplicateMergeFieldAction.NoAction
            }
            : DuplicateMergeFieldAction.NoAction;
}

public enum DuplicateMergeFieldAction
{
    NoAction,
    Copy,
    Merge
}

public sealed class DuplicateCandidateGroupViewModel(DuplicateCandidateGroup group)
{
    public string DisplayTitle { get; } = group.DisplayTitle;
    public string Header => $"{DuplicateCandidatesViewModel.BuildGroupTitle(group)} ({group.Books.Count})";
    public IReadOnlyList<DuplicateCandidateBookViewModel> Books { get; } = group.Books
        .Select(book => new DuplicateCandidateBookViewModel(book))
        .ToList()
        .AsReadOnly();
}

public sealed class DuplicateCandidateBookViewModel(EbookManager.Domain.Books.Book book)
{
    public Guid Id { get; } = book.Id;
    public string Title { get; } = book.Metadata.Title;
    public string Authors { get; } = string.Join(", ", book.Metadata.Authors);
    public string Series { get; } = book.Metadata.Series ?? string.Empty;
    public string Language { get; } = book.Metadata.Language ?? string.Empty;
    public string FormatText { get; } = FormatFormats(book.Formats);
    public string Status { get; } = book.ReadingStatus.ToString();

    private static string FormatFormats(IReadOnlyList<EbookFormat> formats) =>
        formats.Count == 0
            ? string.Empty
            : string.Join(", ", formats.Select(format => format.ToString().ToUpperInvariant()));
}

public sealed partial class DuplicateCandidateRowViewModel : ObservableObject
{
    public DuplicateCandidateRowViewModel(
        string matchKey,
        string groupTitle,
        DuplicateCandidateMatchKind matchKind,
        EbookManager.Domain.Books.Book book,
        string? libraryPath = null)
    {
        Id = book.Id;
        MatchKey = matchKey;
        GroupTitle = groupTitle;
        MatchKind = matchKind;
        Title = book.Metadata.Title;
        Authors = string.Join(", ", book.Metadata.Authors);
        Series = book.Metadata.Series ?? string.Empty;
        SeriesNumber = book.Metadata.SeriesNumber?.ToString(System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;
        Language = book.Metadata.Language ?? string.Empty;
        FormatText = FormatFormats(book.Formats);
        Publisher = book.Metadata.Publisher ?? string.Empty;
        PublicationDate = book.Metadata.PublicationDate?.ToString("d", System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;
        Isbn = book.Metadata.Isbn ?? string.Empty;
        Tags = book.Metadata.Tags is null ? string.Empty : string.Join(", ", book.Metadata.Tags);
        Description = book.Metadata.Description ?? string.Empty;
        Status = book.ReadingStatus.ToString();
        CoverPath = libraryPath is null || string.IsNullOrWhiteSpace(book.CoverRelativePath)
            ? null
            : Path.Combine(libraryPath, book.CoverRelativePath);
    }

    [ObservableProperty]
    private bool isSelected;

    public Guid Id { get; }
    public string MatchKey { get; }
    public string GroupTitle { get; }
    public DuplicateCandidateMatchKind MatchKind { get; }
    public string Title { get; }
    public string Authors { get; }
    public string Series { get; }
    public string SeriesNumber { get; }
    public string Language { get; }
    public string FormatText { get; }
    public string Publisher { get; }
    public string PublicationDate { get; }
    public string Isbn { get; }
    public string Tags { get; }
    public string Description { get; }
    public string Status { get; }
    public string? CoverPath { get; }
    public int MetadataScore =>
        CountIfPresent(Title) +
        CountUsefulAuthors(Authors) +
        CountIfPresent(Series) +
        CountIfPresent(Language) +
        CountIfPresent(Publisher) +
        CountIfPresent(PublicationDate) +
        CountIfPresent(Isbn) +
        CountIfPresent(Tags) +
        CountIfPresent(Description) +
        (CoverPath is null ? 0 : 1);

    private static string FormatFormats(IReadOnlyList<EbookFormat> formats) =>
        formats.Count == 0
            ? string.Empty
            : string.Join(", ", formats.Select(format => format.ToString().ToUpperInvariant()));

    private static int CountIfPresent(string value) =>
        string.IsNullOrWhiteSpace(value) ? 0 : 1;

    private static int CountUsefulAuthors(string authors) =>
        string.IsNullOrWhiteSpace(authors) ||
        string.Equals(authors.Trim(), "Unknown", StringComparison.OrdinalIgnoreCase)
            ? 0
            : 1;
}

using EbookManager.Application.Books;
using EbookManager.Domain.Books;
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
    private readonly Func<DuplicateCandidateRowViewModel, DuplicateCandidateRowViewModel, CancellationToken, Task<bool>>? mergeCandidateAsync;
    private readonly AsyncRelayCommand deleteSelectedCandidatesCommand;
    private IReadOnlyList<Book> books;
    private IReadOnlyList<DuplicateCandidateGroup> allGroups;
    private bool exactMatchesOnly = true;

    public DuplicateCandidatesViewModel(
        DuplicateCandidateResult result,
        string? libraryPath = null,
        Func<DuplicateCandidateRowViewModel, CancellationToken, Task<bool>>? deleteCandidateAsync = null,
        Func<DuplicateCandidateRowViewModel, DuplicateCandidateRowViewModel, CancellationToken, Task<bool>>? mergeCandidateAsync = null)
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
        OnPropertyChanged(nameof(HasChanges));
        ApplyResult(duplicateCandidateService.FindCandidates(books));
    }

    public async Task MergeCandidateAsync(
        DuplicateCandidateRowViewModel sourceRow,
        CancellationToken cancellationToken)
    {
        if (mergeCandidateAsync is null)
        {
            return;
        }

        var mergePair = SelectMergePair(sourceRow);
        if (mergePair is null)
        {
            return;
        }

        var merged = await mergeCandidateAsync(mergePair.Value.Source, mergePair.Value.Target, cancellationToken);
        if (!merged)
        {
            return;
        }

        books = books
            .Where(book => book.Id != mergePair.Value.Source.Id)
            .ToList()
            .AsReadOnly();
        HasChanges = true;
        OnPropertyChanged(nameof(HasChanges));
        ApplyResult(duplicateCandidateService.FindCandidates(books));
    }

    public DuplicateMergePreviewViewModel? CreateMergePreview(DuplicateCandidateRowViewModel clickedRow)
    {
        var mergePair = SelectMergePair(clickedRow);
        return mergePair is null
            ? null
            : new DuplicateMergePreviewViewModel(mergePair.Value.Source, mergePair.Value.Target);
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
        }

        OnPropertyChanged(nameof(HasChanges));
        ApplyResult(duplicateCandidateService.FindCandidates(books));
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

public sealed class DuplicateMergePreviewViewModel(
    DuplicateCandidateRowViewModel source,
    DuplicateCandidateRowViewModel target)
{
    public DuplicateCandidateRowViewModel Source { get; } = source;
    public DuplicateCandidateRowViewModel Target { get; } = target;
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

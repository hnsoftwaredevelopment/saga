using EbookManager.Application.Books;
using EbookManager.Domain.Books;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class DuplicateCandidatesViewModel : ObservableObject
{
    private readonly DuplicateCandidateService duplicateCandidateService = new();
    private readonly string? libraryPath;
    private readonly Func<DuplicateCandidateRowViewModel, CancellationToken, Task<bool>>? deleteCandidateAsync;
    private IReadOnlyList<Book> books;

    public DuplicateCandidatesViewModel(
        DuplicateCandidateResult result,
        string? libraryPath = null,
        Func<DuplicateCandidateRowViewModel, CancellationToken, Task<bool>>? deleteCandidateAsync = null)
    {
        this.libraryPath = libraryPath;
        this.deleteCandidateAsync = deleteCandidateAsync;
        books = result.Groups
            .SelectMany(group => group.Books)
            .DistinctBy(book => book.Id)
            .ToList()
            .AsReadOnly();
        ApplyResult(result);
    }

    public ObservableCollection<DuplicateCandidateGroupViewModel> Groups { get; } = [];
    public ObservableCollection<DuplicateCandidateRowViewModel> Rows { get; } = [];
    public int GroupCount => Groups.Count;
    public int BookCount => Groups.Sum(group => group.Books.Count);
    public bool HasGroups => Groups.Count > 0;
    public string SummaryText => $"{GroupCount} groups, {BookCount} books";
    public bool HasChanges { get; private set; }

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

    public static string BuildGroupTitle(DuplicateCandidateGroup group) =>
        string.IsNullOrWhiteSpace(group.AuthorSummary)
            ? group.DisplayTitle
            : $"{group.DisplayTitle} - {group.AuthorSummary}";

    private void ApplyResult(DuplicateCandidateResult result)
    {
        Groups.Clear();
        Rows.Clear();
        foreach (var group in result.Groups)
        {
            Groups.Add(new DuplicateCandidateGroupViewModel(group));
            foreach (var book in group.Books)
            {
                Rows.Add(new DuplicateCandidateRowViewModel(BuildGroupTitle(group), book, libraryPath));
            }
        }

        OnPropertyChanged(nameof(GroupCount));
        OnPropertyChanged(nameof(BookCount));
        OnPropertyChanged(nameof(HasGroups));
        OnPropertyChanged(nameof(SummaryText));
    }
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
    public string Status { get; } = book.ReadingStatus.ToString();
}

public sealed class DuplicateCandidateRowViewModel(
    string groupTitle,
    EbookManager.Domain.Books.Book book,
    string? libraryPath = null)
{
    public Guid Id { get; } = book.Id;
    public string GroupTitle { get; } = groupTitle;
    public string Title { get; } = book.Metadata.Title;
    public string Authors { get; } = string.Join(", ", book.Metadata.Authors);
    public string Series { get; } = book.Metadata.Series ?? string.Empty;
    public string SeriesNumber { get; } = book.Metadata.SeriesNumber?.ToString(System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;
    public string Language { get; } = book.Metadata.Language ?? string.Empty;
    public string Publisher { get; } = book.Metadata.Publisher ?? string.Empty;
    public string PublicationDate { get; } = book.Metadata.PublicationDate?.ToString("d", System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;
    public string Isbn { get; } = book.Metadata.Isbn ?? string.Empty;
    public string Tags { get; } = book.Metadata.Tags is null ? string.Empty : string.Join(", ", book.Metadata.Tags);
    public string Description { get; } = book.Metadata.Description ?? string.Empty;
    public string Status { get; } = book.ReadingStatus.ToString();
    public string? CoverPath { get; } = libraryPath is null || string.IsNullOrWhiteSpace(book.CoverRelativePath)
        ? null
        : Path.Combine(libraryPath, book.CoverRelativePath);
}

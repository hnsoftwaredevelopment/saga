using EbookManager.Application.Books;

namespace EbookManager.Presentation.ViewModels;

public sealed class DuplicateCandidatesViewModel(DuplicateCandidateResult result)
{
    public IReadOnlyList<DuplicateCandidateGroupViewModel> Groups { get; } = result.Groups
        .Select(group => new DuplicateCandidateGroupViewModel(group))
        .ToList()
        .AsReadOnly();

    public IReadOnlyList<DuplicateCandidateRowViewModel> Rows { get; } = result.Groups
        .SelectMany(group => group.Books.Select(book => new DuplicateCandidateRowViewModel(BuildGroupTitle(group), book)))
        .ToList()
        .AsReadOnly();

    public int GroupCount => Groups.Count;
    public int BookCount => Groups.Sum(group => group.Books.Count);
    public bool HasGroups => Groups.Count > 0;
    public string SummaryText => $"{GroupCount} groups, {BookCount} books";

    public static string BuildGroupTitle(DuplicateCandidateGroup group) =>
        string.IsNullOrWhiteSpace(group.AuthorSummary)
            ? group.DisplayTitle
            : $"{group.DisplayTitle} - {group.AuthorSummary}";
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

public sealed class DuplicateCandidateRowViewModel(string groupTitle, EbookManager.Domain.Books.Book book)
{
    public Guid Id { get; } = book.Id;
    public string GroupTitle { get; } = groupTitle;
    public string Title { get; } = book.Metadata.Title;
    public string Authors { get; } = string.Join(", ", book.Metadata.Authors);
    public string Series { get; } = book.Metadata.Series ?? string.Empty;
    public string Language { get; } = book.Metadata.Language ?? string.Empty;
    public string Status { get; } = book.ReadingStatus.ToString();
}

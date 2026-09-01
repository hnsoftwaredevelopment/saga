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
    private readonly AsyncRelayCommand markSelectedIssueCorrectCommand;

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
        IMetadataQualityExclusionRepository? repository = null)
    {
        this.localize = localize;
        this.repository = repository;
        markSelectedIssueCorrectCommand = new AsyncRelayCommand(
            MarkSelectedIssueCorrectAsync,
            CanMarkSelectedIssueCorrect);
        TotalBookCount = books.Count;
        Issues = new ObservableCollection<MetadataQualityIssueViewModel>(
            BuildIssues(
                books,
                localize,
                exclusions ?? new HashSet<MetadataQualityExclusionKey>()));
        SelectedIssue = Issues.FirstOrDefault(issue => issue.Count > 0) ?? Issues.FirstOrDefault();
    }

    public int TotalBookCount { get; }
    public ObservableCollection<MetadataQualityIssueViewModel> Issues { get; }
    public bool HasIssues => Issues.Any(issue => issue.Count > 0);
    public int TotalIssueCount => Issues.Sum(issue => issue.Count);
    public Guid? SelectedBookId => SelectedBook?.Id;
    public bool CanOpenSelectedBook => SelectedBook is not null;
    public IAsyncRelayCommand MarkSelectedIssueCorrectCommand => markSelectedIssueCorrectCommand;

    partial void OnSelectedIssueChanged(MetadataQualityIssueViewModel? value)
    {
        SelectedBook = value?.Rows.FirstOrDefault();
        markSelectedIssueCorrectCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedBookChanged(MetadataQualityBookRowViewModel? value) =>
        markSelectedIssueCorrectCommand.NotifyCanExecuteChanged();

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

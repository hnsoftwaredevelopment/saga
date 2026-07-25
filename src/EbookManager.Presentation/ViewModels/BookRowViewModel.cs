using System.Globalization;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;
using EbookManager.Domain.Settings;

namespace EbookManager.Presentation.ViewModels;

public sealed class BookRowViewModel(
    Book book,
    string searchText = "",
    string? libraryPath = null,
    AuthorSortStrategy authorSortStrategy = AuthorSortStrategy.DisplayName,
    string? primaryGroupName = null,
    string? secondaryGroupName = null)
{
    public Book Book { get; } = book;
    public Guid Id => Book.Id;
    public string Title => Book.Metadata.Title;
    public string Authors => string.Join(", ", Book.Metadata.Authors);
    public string AuthorsSortKey => AuthorSortKeyBuilder.BuildSortKey(Authors, authorSortStrategy);
    public string Description => Book.Metadata.Description ?? string.Empty;
    public string Series => Book.Metadata.Series ?? string.Empty;
    public decimal? SeriesNumber => Book.Metadata.SeriesNumber;
    public string SeriesNumberText => SeriesNumber?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
    public string Publisher => Book.Metadata.Publisher ?? string.Empty;
    public string PublicationDate => Book.Metadata.PublicationDate?.ToString("d", CultureInfo.CurrentCulture) ?? string.Empty;
    public string Tags => string.Join(", ", Book.Metadata.Tags ?? []);
    public string Isbn => Book.Metadata.Isbn ?? string.Empty;
    public string Language => string.IsNullOrWhiteSpace(Book.Metadata.Language)
        ? string.Empty
        : LanguageDisplayService.DisplayName(Book.Metadata.Language);
    public string Formats => string.Join(", ", Book.Formats.Select(format => format.ToString().ToUpperInvariant()));
    public string GroupName { get; } = primaryGroupName ?? string.Empty;
    public string PrimaryGroupName { get; } = primaryGroupName ?? string.Empty;
    public string SecondaryGroupName { get; } = secondaryGroupName ?? string.Empty;
    public string BookshelfGroupHeader =>
        (string.IsNullOrWhiteSpace(PrimaryGroupName), string.IsNullOrWhiteSpace(SecondaryGroupName)) switch
        {
            (true, true) => string.Empty,
            (true, false) => SecondaryGroupName,
            (false, true) => PrimaryGroupName,
            _ => $"{PrimaryGroupName} / {SecondaryGroupName}"
        };
    public ReadingStatus ReadingStatus => Book.ReadingStatus;
    public string DateAdded => FormatDateTime(Book.CreatedUtc);
    public string LastModified => FormatDateTime(Book.UpdatedUtc);
    public string EReader => "Unavailable";
    public byte[]? CoverBytes => Book.Metadata.CoverBytes;
    public string? CoverRelativePath => Book.CoverRelativePath;
    public string? CoverPath => libraryPath is null || string.IsNullOrWhiteSpace(Book.CoverRelativePath)
        ? null
        : Path.Combine(libraryPath, Book.CoverRelativePath);
    public string SearchText { get; } = searchText;

    private static string FormatDateTime(DateTimeOffset value) =>
        value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
}

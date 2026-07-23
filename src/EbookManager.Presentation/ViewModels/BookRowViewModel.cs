using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;

namespace EbookManager.Presentation.ViewModels;

public sealed class BookRowViewModel(Book book, string searchText = "", string? libraryPath = null)
{
    public Book Book { get; } = book;
    public Guid Id => Book.Id;
    public string Title => Book.Metadata.Title;
    public string Authors => string.Join(", ", Book.Metadata.Authors);
    public string Series => Book.Metadata.Series ?? string.Empty;
    public string Language => string.IsNullOrWhiteSpace(Book.Metadata.Language)
        ? string.Empty
        : LanguageDisplayService.DisplayName(Book.Metadata.Language);
    public string Formats => string.Join(", ", Book.Formats.Select(format => format.ToString().ToUpperInvariant()));
    public ReadingStatus ReadingStatus => Book.ReadingStatus;
    public string EReader => "Unavailable";
    public byte[]? CoverBytes => Book.Metadata.CoverBytes;
    public string? CoverRelativePath => Book.CoverRelativePath;
    public string? CoverPath => libraryPath is null || string.IsNullOrWhiteSpace(Book.CoverRelativePath)
        ? null
        : Path.Combine(libraryPath, Book.CoverRelativePath);
    public string SearchText { get; } = searchText;
}

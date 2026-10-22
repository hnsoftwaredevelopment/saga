using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed class MetadataQualityTitleAuthorRepairViewModel
{
    public MetadataQualityTitleAuthorRepairViewModel(string currentTitle, string currentAuthor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentTitle);
        if (!MetadataQualityAuthorRules.IsUsable(currentAuthor))
        {
            throw new ArgumentException("A usable author is required.", nameof(currentAuthor));
        }

        CurrentTitle = currentTitle.Trim();
        CurrentAuthor = currentAuthor.Trim();
    }

    public MetadataQualityTitleAuthorRepairViewModel(IReadOnlyList<Book> books, Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(books);
        ArgumentNullException.ThrowIfNull(localize);
        if (books.Count < 2 || books.Select(book => book.Id).Distinct().Count() != books.Count)
        {
            throw new ArgumentException("At least two distinct books are required.", nameof(books));
        }

        CurrentTitle = string.Empty;
        CurrentAuthor = string.Empty;
        Previews = books.Select(book => new MetadataQualityTitleAuthorRepairRowViewModel(book)).ToArray();
        BulkConfirmationMessage = string.Format(CultureInfo.CurrentCulture,
            localize(EligibleCount == books.Count
                ? "MetadataQualityTitleAuthorRepairBulkConfirmationMessage"
                : "MetadataQualityTitleAuthorRepairBulkConfirmationSkipped"),
            books.Count, EligibleCount, books.Count - EligibleCount);
    }

    public IReadOnlyList<MetadataQualityTitleAuthorRepairRowViewModel> Previews { get; } = [];
    public bool IsBulk => Previews.Count > 1;
    public int EligibleCount => Previews.Count(row => row.CanRepair);
    public bool CanConfirm => !IsBulk || EligibleCount > 0;
    public string? BulkConfirmationMessage { get; }

    public string CurrentTitle { get; }
    public string CurrentAuthor { get; }
    public string NewTitle => CurrentAuthor;
    public string NewAuthor => CurrentTitle;
}

public sealed class MetadataQualityTitleAuthorRepairRowViewModel
{
    public MetadataQualityTitleAuthorRepairRowViewModel(Book book)
    {
        BookId = book.Id;
        Snapshot = new(book.Metadata.Title, Array.AsReadOnly(book.Metadata.Authors.ToArray()));
        CanRepair = MetadataQualityTitleAuthorRepairRules.CanRepair(book);
    }

    public Guid BookId { get; }
    public MetadataQualityTitleAuthorSnapshot Snapshot { get; }
    public bool CanRepair { get; }
    public string CurrentTitle => Snapshot.Title;
    public string CurrentAuthor => string.Join("; ", Snapshot.Authors);
    public string NewTitle => CanRepair ? Snapshot.Authors[0].Trim() : string.Empty;
    public string NewAuthor => CanRepair ? Snapshot.Title.Trim() : string.Empty;
}

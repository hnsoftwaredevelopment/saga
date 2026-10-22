using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed class MetadataQualityTitleAuthorRepairViewModel : ObservableObject
{
    private readonly Func<string, string>? localize;
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

        this.localize = localize;
        CurrentTitle = string.Empty;
        CurrentAuthor = string.Empty;
        Previews = books.Select(book => new MetadataQualityTitleAuthorRepairRowViewModel(book)).ToArray();
        foreach (var preview in Previews)
        {
            preview.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(preview.IsSelected))
                {
                    OnPropertyChanged(nameof(SelectedCount));
                    OnPropertyChanged(nameof(CanConfirm));
                    OnPropertyChanged(nameof(BulkConfirmText));
                }
            };
        }

        BulkContext = string.Format(CultureInfo.CurrentCulture,
            localize("MetadataQualityTitleAuthorRepairBulkContext"), books.Count);
    }

    public IReadOnlyList<MetadataQualityTitleAuthorRepairRowViewModel> Previews { get; } = [];
    public bool IsBulk => Previews.Count > 1;
    public int SelectedCount => Previews.Count(row => row.CanRepair && row.IsSelected);
    public bool CanConfirm => !IsBulk || SelectedCount > 0;
    public string? BulkContext { get; }
    public string? BulkConfirmText => localize is null ? null : string.Format(CultureInfo.CurrentCulture,
        localize("MetadataQualityTitleAuthorRepairBulkConfirm"), SelectedCount);

    public string CurrentTitle { get; }
    public string CurrentAuthor { get; }
    public string NewTitle => CurrentAuthor;
    public string NewAuthor => CurrentTitle;
}

public sealed partial class MetadataQualityTitleAuthorRepairRowViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isSelected;

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

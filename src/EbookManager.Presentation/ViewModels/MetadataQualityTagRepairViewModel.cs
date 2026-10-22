using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Application.Metadata;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class MetadataQualityTagRepairViewModel : ObservableObject
{
    private readonly IReadOnlyList<string> storedTags;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NormalizedTags))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string tagsText;

    public MetadataQualityTagRepairViewModel(
        string bookTitle,
        IReadOnlyList<string>? currentTags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookTitle);

        BookTitle = bookTitle;
        storedTags = currentTags?.ToArray() ?? [];
        CurrentTagsText = string.Join(Environment.NewLine, storedTags);
        tagsText = string.Join(
            Environment.NewLine,
            MetadataQualityTagNormalizer.Normalize(storedTags));
    }

    public MetadataQualityTagRepairViewModel(
        IReadOnlyList<(Guid BookId, string Title, IReadOnlyList<string>? Tags)> books,
        Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(books);
        ArgumentNullException.ThrowIfNull(localize);
        if (books.Count < 2 || books.Select(book => book.BookId).Distinct().Count() != books.Count)
        {
            throw new ArgumentException("At least two distinct books are required.", nameof(books));
        }

        Previews = books.Select(book => new MetadataQualityTagRepairPreview(
            book.BookId,
            book.Title,
            Array.AsReadOnly((book.Tags ?? []).ToArray()),
            Array.AsReadOnly(MetadataQualityTagNormalizer.Normalize(book.Tags ?? []).ToArray())))
            .ToArray();
        BulkContext = string.Format(CultureInfo.CurrentCulture,
            localize("MetadataQualityTagRepairBulkContext"), books.Count);
        BulkSaveText = string.Format(CultureInfo.CurrentCulture,
            localize("MetadataQualityTagRepairBulkSave"), books.Count);
        BookTitle = string.Empty;
        CurrentTagsText = string.Empty;
        tagsText = string.Empty;
        storedTags = [];
    }

    public IReadOnlyList<MetadataQualityTagRepairPreview> Previews { get; } = [];
    public bool IsBulk => Previews.Count > 1;
    public string? BulkContext { get; }
    public string? BulkSaveText { get; }

    public string BookTitle { get; }
    public string CurrentTagsText { get; }
    public IReadOnlyList<string> NormalizedTags =>
        MetadataQualityTagNormalizer.Normalize([TagsText]);
    public bool CanSave => IsBulk ? Previews.Any(preview =>
        !preview.OriginalTags.SequenceEqual(preview.Tags, StringComparer.CurrentCultureIgnoreCase))
        : !storedTags.SequenceEqual(
        NormalizedTags,
        StringComparer.CurrentCultureIgnoreCase);
}

public sealed record MetadataQualityTagRepairPreview(
    Guid BookId,
    string BookTitle,
    IReadOnlyList<string> OriginalTags,
    IReadOnlyList<string> Tags)
{
    public string CurrentTagsText => string.Join(Environment.NewLine, OriginalTags);
    public string ProposedTagsText => string.Join(Environment.NewLine, Tags);
}

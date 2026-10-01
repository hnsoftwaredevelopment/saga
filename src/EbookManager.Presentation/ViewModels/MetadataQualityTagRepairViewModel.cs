using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Application.Metadata;

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

    public string BookTitle { get; }
    public string CurrentTagsText { get; }
    public IReadOnlyList<string> NormalizedTags =>
        MetadataQualityTagNormalizer.Normalize([TagsText]);
    public bool CanSave => !storedTags.SequenceEqual(
        NormalizedTags,
        StringComparer.CurrentCultureIgnoreCase);
}

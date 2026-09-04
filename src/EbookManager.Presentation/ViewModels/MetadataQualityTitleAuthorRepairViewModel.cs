using EbookManager.Application.Metadata;

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

    public string CurrentTitle { get; }
    public string CurrentAuthor { get; }
    public string NewTitle => CurrentAuthor;
    public string NewAuthor => CurrentTitle;
}

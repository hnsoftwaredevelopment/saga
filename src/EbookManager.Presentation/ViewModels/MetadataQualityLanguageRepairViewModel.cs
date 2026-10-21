using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Application.Metadata;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class MetadataQualityLanguageRepairViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(NormalizedLanguage))]
    private MetadataQualityLanguageOption? selectedLanguage;

    public MetadataQualityLanguageRepairViewModel(string bookTitle)
        : this([bookTitle], key => key)
    {
    }

    public MetadataQualityLanguageRepairViewModel(
        IReadOnlyCollection<string> bookTitles,
        Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(bookTitles);
        ArgumentNullException.ThrowIfNull(localize);

        if (bookTitles.Count == 0)
        {
            throw new ArgumentException("At least one book title is required.", nameof(bookTitles));
        }

        AffectedBookCount = bookTitles.Count;
        BookTitle = AffectedBookCount == 1 ? bookTitles.Single() : null;
        ContextLabel = localize(AffectedBookCount == 1 ? "Title" : "MetadataMultiEditSelectedBooks");
        ContextText = BookTitle ?? string.Format(
            CultureInfo.CurrentCulture,
            localize("MetadataQualityLanguageRepairBulkBookContext"),
            AffectedBookCount);
        SaveButtonText = AffectedBookCount == 1
            ? localize("MetadataQualityLanguageRepairSave")
            : string.Format(
                CultureInfo.CurrentCulture,
                localize("MetadataQualityLanguageRepairBulkSave"),
                AffectedBookCount);
        Languages = CultureInfo
            .GetCultures(CultureTypes.NeutralCultures)
            .Select(culture => MetadataQualityLanguageRules.Normalize(culture.Name))
            .Where(code => code is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(code => new MetadataQualityLanguageOption(
                code!,
                LanguageDisplayService.DisplayName(code!)))
            .OrderBy(option => option.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(option => option.Code, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public int AffectedBookCount { get; }
    public string? BookTitle { get; }
    public string ContextLabel { get; }
    public string ContextText { get; }
    public string SaveButtonText { get; }
    public IReadOnlyList<MetadataQualityLanguageOption> Languages { get; }
    public string? NormalizedLanguage => SelectedLanguage?.Code;
    public bool CanSave => NormalizedLanguage is not null;
}

public sealed record MetadataQualityLanguageOption(string Code, string DisplayName)
{
    public string DisplayText => $"{DisplayName} ({Code})";
}

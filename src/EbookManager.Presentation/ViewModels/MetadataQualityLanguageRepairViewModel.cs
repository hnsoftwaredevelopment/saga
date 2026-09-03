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
    {
        BookTitle = bookTitle;
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

    public string BookTitle { get; }
    public IReadOnlyList<MetadataQualityLanguageOption> Languages { get; }
    public string? NormalizedLanguage => SelectedLanguage?.Code;
    public bool CanSave => NormalizedLanguage is not null;
}

public sealed record MetadataQualityLanguageOption(string Code, string DisplayName)
{
    public string DisplayText => $"{DisplayName} ({Code})";
}

using System.Globalization;
using System.Resources;
using EbookManager.App.Localization;
using EbookManager.Domain.Abstractions;

namespace EbookManager.App.Services;

public sealed class LocalizationService(IAppSettingsStore settingsStore)
{
    private static readonly ResourceManager Resources = new(
        "EbookManager.App.Resources.Strings.AppResources",
        typeof(LocalizationService).Assembly);

    private static readonly IReadOnlySet<string> SupportedCultures = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "en-US",
        "nl-NL",
        "de-DE",
        "fr-FR",
        "es-ES",
        "it-IT"
    };

    private readonly IAppSettingsStore settingsStore = settingsStore;

    public IReadOnlyList<CultureInfo> SelectableCultures { get; } =
    [
        CultureInfo.GetCultureInfo("en-US"),
        CultureInfo.GetCultureInfo("nl-NL"),
        CultureInfo.GetCultureInfo("de-DE"),
        CultureInfo.GetCultureInfo("fr-FR"),
        CultureInfo.GetCultureInfo("es-ES"),
        CultureInfo.GetCultureInfo("it-IT")
    ];

    public async Task ApplySavedCultureAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsStore.LoadAsync(cancellationToken);
        ApplyCulture(settings.Culture);
    }

    public void ApplyCulture(string cultureName)
    {
        var culture = SupportedCultures.Contains(cultureName)
            ? CultureInfo.GetCultureInfo(cultureName)
            : CultureInfo.GetCultureInfo("en-US");

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        LocalizedStrings.Current.Refresh();
    }

    public string GetString(string key) =>
        Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}

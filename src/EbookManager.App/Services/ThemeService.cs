using EbookManager.Domain.Abstractions;

namespace EbookManager.App.Services;

public sealed class ThemeService(IAppSettingsStore settingsStore)
{
    private const string ThemeDictionaryMarker = "EbookManagerTheme";
    private readonly IAppSettingsStore settingsStore = settingsStore;

    public IReadOnlyList<string> SelectableThemes { get; } = ["Light", "Dark", "Sepia", "Blue", "Red"];

    public async Task ApplySavedThemeAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsStore.LoadAsync(cancellationToken);
        ApplyTheme(settings.Theme);
    }

    public void ApplyTheme(string theme)
    {
        var normalizedTheme = SelectableThemes.FirstOrDefault(
            selectableTheme => string.Equals(selectableTheme, theme, StringComparison.OrdinalIgnoreCase)) ?? "Light";
        var application = System.Windows.Application.Current;
        if (application is null)
        {
            return;
        }

        var existing = application.Resources.MergedDictionaries
            .FirstOrDefault(dictionary =>
                dictionary.Contains(ThemeDictionaryMarker) &&
                dictionary[ThemeDictionaryMarker] is true);
        if (existing is not null)
        {
            application.Resources.MergedDictionaries.Remove(existing);
        }

        var dictionary = new System.Windows.ResourceDictionary
        {
            Source = new Uri(
                $"/EbookManager;component/Resources/Styles/Themes/{normalizedTheme}Theme.xaml",
                UriKind.Relative)
        };
        application.Resources.MergedDictionaries.Add(dictionary);
    }
}

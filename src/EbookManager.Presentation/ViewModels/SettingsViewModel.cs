using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Settings;

namespace EbookManager.Presentation.ViewModels;

public sealed record AuthorSortStrategyOption(AuthorSortStrategy Value, string ResourceKey);

public sealed partial class SettingsViewModel(IAppSettingsStore settingsStore) : ObservableObject
{
    private readonly IAppSettingsStore settingsStore = settingsStore;

    public IReadOnlyList<string> SelectableThemes { get; } = ["Light", "Dark", "Sepia", "Blue", "Red"];
    public IReadOnlyList<AuthorSortStrategyOption> SelectableAuthorSortStrategies { get; } =
    [
        new(AuthorSortStrategy.DisplayName, "AuthorSortDisplayName"),
        new(AuthorSortStrategy.LastNameFirst, "AuthorSortLastNameFirst"),
        new(AuthorSortStrategy.LastNameFirstDutchPrefixes, "AuthorSortLastNameFirstDutchPrefixes")
    ];

    [ObservableProperty]
    private string culture = "en-US";

    [ObservableProperty]
    private string theme = "Light";

    [ObservableProperty]
    private string defaultView = "Detailed";

    [ObservableProperty]
    private bool confirmDelete = true;

    [ObservableProperty]
    private bool includeScanSubdirectories = true;

    [ObservableProperty]
    private AuthorSortStrategy authorSortStrategy = AuthorSortStrategy.DisplayName;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await settingsStore.LoadAsync(cancellationToken);
        Culture = settings.Culture;
        Theme = settings.Theme;
        DefaultView = settings.DefaultView;
        ConfirmDelete = settings.ConfirmDelete;
        IncludeScanSubdirectories = settings.IncludeScanSubdirectories;
        AuthorSortStrategy = settings.AuthorSortStrategy;
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var current = await settingsStore.LoadAsync(cancellationToken);
        await settingsStore.SaveAsync(
            current with
            {
                Culture = Culture,
                Theme = Theme,
                DefaultView = DefaultView,
                ConfirmDelete = ConfirmDelete,
                IncludeScanSubdirectories = IncludeScanSubdirectories,
                AuthorSortStrategy = AuthorSortStrategy
            },
            cancellationToken);
    }
}

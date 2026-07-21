using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Settings;

namespace EbookManager.Presentation.ViewModels;

public sealed record AuthorSortStrategyOption(AuthorSortStrategy Value, string ResourceKey);
public sealed record CultureOption(string Value, string DisplayName);
public sealed record DuplicateMergeDefaultActionOption(DuplicateMergeDefaultAction Value, string ResourceKey);

public sealed partial class SettingsViewModel(IAppSettingsStore settingsStore) : ObservableObject
{
    private readonly IAppSettingsStore settingsStore = settingsStore;

    public IReadOnlyList<CultureOption> SelectableCultures { get; } =
    [
        new("en-US", "English (US)"),
        new("nl-NL", "Nederlands"),
        new("de-DE", "Deutsch"),
        new("fr-FR", "Français"),
        new("es-ES", "Español"),
        new("it-IT", "Italiano")
    ];

    public IReadOnlyList<string> SelectableThemes { get; } = ["Light", "Dark", "Sepia", "Blue", "Red"];
    public IReadOnlyList<AuthorSortStrategyOption> SelectableAuthorSortStrategies { get; } =
    [
        new(AuthorSortStrategy.DisplayName, "AuthorSortDisplayName"),
        new(AuthorSortStrategy.LastNameFirst, "AuthorSortLastNameFirst"),
        new(AuthorSortStrategy.LastNameFirstDutchPrefixes, "AuthorSortLastNameFirstDutchPrefixes")
    ];
    public IReadOnlyList<DuplicateMergeDefaultActionOption> SelectableDuplicateMergeDefaultActions { get; } =
    [
        new(DuplicateMergeDefaultAction.NoAction, "MergeActionNoAction"),
        new(DuplicateMergeDefaultAction.Copy, "MergeActionCopy"),
        new(DuplicateMergeDefaultAction.Merge, "MergeActionMerge")
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

    [ObservableProperty]
    private bool duplicateExactMatchesOnly = true;

    [ObservableProperty]
    private bool enableDiagnosticDetails = true;

    [ObservableProperty]
    private DuplicateMergeDefaultAction mergeDefaultCover = DuplicateMergeDefaultAction.NoAction;

    [ObservableProperty]
    private DuplicateMergeDefaultAction mergeDefaultAuthors = DuplicateMergeDefaultAction.Merge;

    [ObservableProperty]
    private DuplicateMergeDefaultAction mergeDefaultTags = DuplicateMergeDefaultAction.Merge;

    [ObservableProperty]
    private DuplicateMergeDefaultAction mergeDefaultDescription = DuplicateMergeDefaultAction.NoAction;

    [ObservableProperty]
    private DuplicateMergeDefaultAction mergeDefaultPublisher = DuplicateMergeDefaultAction.NoAction;

    [ObservableProperty]
    private DuplicateMergeDefaultAction mergeDefaultLanguage = DuplicateMergeDefaultAction.NoAction;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await settingsStore.LoadAsync(cancellationToken);
        var mergeDefaults = settings.DuplicateMergeDefaults ?? new DuplicateMergeDefaultSettings();
        Culture = settings.Culture;
        Theme = settings.Theme;
        DefaultView = settings.DefaultView;
        ConfirmDelete = settings.ConfirmDelete;
        IncludeScanSubdirectories = settings.IncludeScanSubdirectories;
        AuthorSortStrategy = settings.AuthorSortStrategy;
        DuplicateExactMatchesOnly = settings.DuplicateExactMatchesOnly;
        EnableDiagnosticDetails = settings.EnableDiagnosticDetails;
        MergeDefaultCover = mergeDefaults.Cover;
        MergeDefaultAuthors = mergeDefaults.Authors;
        MergeDefaultTags = mergeDefaults.Tags;
        MergeDefaultDescription = mergeDefaults.Description;
        MergeDefaultPublisher = mergeDefaults.Publisher;
        MergeDefaultLanguage = mergeDefaults.Language;
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
                AuthorSortStrategy = AuthorSortStrategy,
                DuplicateExactMatchesOnly = DuplicateExactMatchesOnly,
                EnableDiagnosticDetails = EnableDiagnosticDetails,
                DuplicateMergeDefaults = (current.DuplicateMergeDefaults ?? new DuplicateMergeDefaultSettings()) with
                {
                    Cover = MergeDefaultCover,
                    Authors = MergeDefaultAuthors,
                    Tags = MergeDefaultTags,
                    Description = MergeDefaultDescription,
                    Publisher = MergeDefaultPublisher,
                    Language = MergeDefaultLanguage
                }
            },
            cancellationToken);
    }
}

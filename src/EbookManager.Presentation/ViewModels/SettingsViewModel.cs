using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.CustomMetadata;
using EbookManager.Domain.Settings;

namespace EbookManager.Presentation.ViewModels;

public sealed record AuthorSortStrategyOption(AuthorSortStrategy Value, string ResourceKey);
public sealed record CultureOption(string Value, string DisplayName);
public sealed record CustomMetadataFieldTypeOption(CustomMetadataFieldType Value, string ResourceKey);
public sealed record DuplicateMergeDefaultActionOption(DuplicateMergeDefaultAction Value, string ResourceKey);

public sealed partial class SettingsViewModel(
    IAppSettingsStore settingsStore,
    ICustomMetadataRepository? customMetadataRepository = null) : ObservableObject
{
    private readonly IAppSettingsStore settingsStore = settingsStore;
    private readonly ICustomMetadataRepository? customMetadataRepository = customMetadataRepository;

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
    public IReadOnlyList<CustomMetadataFieldTypeOption> SelectableCustomMetadataFieldTypes { get; } =
    [
        new(CustomMetadataFieldType.Text, "CustomMetadataFieldTypeText"),
        new(CustomMetadataFieldType.Number, "CustomMetadataFieldTypeNumber"),
        new(CustomMetadataFieldType.Date, "CustomMetadataFieldTypeDate"),
        new(CustomMetadataFieldType.Boolean, "CustomMetadataFieldTypeBoolean"),
        new(CustomMetadataFieldType.SingleSelect, "CustomMetadataFieldTypeSingleSelect"),
        new(CustomMetadataFieldType.MultiSelect, "CustomMetadataFieldTypeMultiSelect")
    ];

    public ObservableCollection<CustomMetadataFieldViewModel> CustomMetadataFields { get; } = [];

    public event EventHandler? CustomMetadataFieldsChanged;

    public bool HasCustomMetadataRepository => customMetadataRepository is not null;

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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomMetadataFieldCommand))]
    private string newCustomMetadataFieldName = string.Empty;

    [ObservableProperty]
    private CustomMetadataFieldType newCustomMetadataFieldType = CustomMetadataFieldType.Text;

    [ObservableProperty]
    private CustomMetadataFieldViewModel? selectedCustomMetadataField;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RenameCustomMetadataFieldCommand))]
    private string customMetadataFieldName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCustomMetadataOptionsCommand))]
    private string customMetadataOptionsText = string.Empty;

    public bool CanEditCustomMetadataOptions =>
        SelectedCustomMetadataField?.CanHaveOptions == true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomMetadataFieldCommand))]
    [NotifyCanExecuteChangedFor(nameof(RenameCustomMetadataFieldCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCustomMetadataFieldCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCustomMetadataOptionsCommand))]
    private bool isCustomMetadataBusy;

    [ObservableProperty]
    private string customMetadataStatusMessage = string.Empty;

    public IAsyncRelayCommand AddCustomMetadataFieldCommand =>
        addCustomMetadataFieldCommand ??= new AsyncRelayCommand(AddCustomMetadataFieldAsync, CanAddCustomMetadataField);

    public IAsyncRelayCommand RenameCustomMetadataFieldCommand =>
        renameCustomMetadataFieldCommand ??= new AsyncRelayCommand(RenameCustomMetadataFieldAsync, CanRenameCustomMetadataField);

    public IAsyncRelayCommand DeleteCustomMetadataFieldCommand =>
        deleteCustomMetadataFieldCommand ??= new AsyncRelayCommand(DeleteCustomMetadataFieldAsync, CanDeleteCustomMetadataField);

    public IAsyncRelayCommand SaveCustomMetadataOptionsCommand =>
        saveCustomMetadataOptionsCommand ??= new AsyncRelayCommand(SaveCustomMetadataOptionsAsync, CanSaveCustomMetadataOptions);

    private AsyncRelayCommand? addCustomMetadataFieldCommand;
    private AsyncRelayCommand? renameCustomMetadataFieldCommand;
    private AsyncRelayCommand? deleteCustomMetadataFieldCommand;
    private AsyncRelayCommand? saveCustomMetadataOptionsCommand;

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
        await LoadCustomMetadataFieldsAsync(cancellationToken);
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

    partial void OnSelectedCustomMetadataFieldChanged(CustomMetadataFieldViewModel? value)
    {
        CustomMetadataFieldName = value?.Name ?? string.Empty;
        CustomMetadataOptionsText = value is null
            ? string.Empty
            : string.Join(Environment.NewLine, value.Options);
        OnPropertyChanged(nameof(CanEditCustomMetadataOptions));
        RenameCustomMetadataFieldCommand.NotifyCanExecuteChanged();
        DeleteCustomMetadataFieldCommand.NotifyCanExecuteChanged();
        SaveCustomMetadataOptionsCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadCustomMetadataFieldsAsync(CancellationToken cancellationToken)
    {
        CustomMetadataFields.Clear();
        if (customMetadataRepository is null)
        {
            return;
        }

        foreach (var definition in await customMetadataRepository.ListDefinitionsAsync(cancellationToken))
        {
            CustomMetadataFields.Add(new CustomMetadataFieldViewModel(definition));
        }
    }

    private bool CanAddCustomMetadataField() =>
        customMetadataRepository is not null &&
        !IsCustomMetadataBusy &&
        !string.IsNullOrWhiteSpace(NewCustomMetadataFieldName);

    private async Task AddCustomMetadataFieldAsync()
    {
        if (customMetadataRepository is null)
        {
            return;
        }

        await ExecuteCustomMetadataOperationAsync(async () =>
        {
            var definition = await customMetadataRepository.AddDefinitionAsync(
                NewCustomMetadataFieldName,
                NewCustomMetadataFieldType,
                default);
            var item = new CustomMetadataFieldViewModel(definition);
            CustomMetadataFields.Add(item);
            SelectedCustomMetadataField = item;
            NewCustomMetadataFieldName = string.Empty;
            CustomMetadataStatusMessage = "CustomMetadataFieldAdded";
            CustomMetadataFieldsChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    private bool CanRenameCustomMetadataField() =>
        customMetadataRepository is not null &&
        !IsCustomMetadataBusy &&
        SelectedCustomMetadataField is not null &&
        !string.IsNullOrWhiteSpace(CustomMetadataFieldName);

    private async Task RenameCustomMetadataFieldAsync()
    {
        if (customMetadataRepository is null || SelectedCustomMetadataField is null)
        {
            return;
        }

        var fieldId = SelectedCustomMetadataField.Id;
        await ExecuteCustomMetadataOperationAsync(async () =>
        {
            await customMetadataRepository.RenameDefinitionAsync(
                fieldId,
                CustomMetadataFieldName,
                default);
            await LoadCustomMetadataFieldsAsync(default);
            SelectedCustomMetadataField = CustomMetadataFields.FirstOrDefault(field => field.Id == fieldId);
            CustomMetadataStatusMessage = "CustomMetadataFieldRenamed";
            CustomMetadataFieldsChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    private bool CanSaveCustomMetadataOptions() =>
        customMetadataRepository is not null &&
        !IsCustomMetadataBusy &&
        SelectedCustomMetadataField?.CanHaveOptions == true;

    private async Task SaveCustomMetadataOptionsAsync()
    {
        if (customMetadataRepository is null || SelectedCustomMetadataField is null)
        {
            return;
        }

        var fieldId = SelectedCustomMetadataField.Id;
        await ExecuteCustomMetadataOperationAsync(async () =>
        {
            await customMetadataRepository.UpdateDefinitionOptionsAsync(
                fieldId,
                ParseOptions(CustomMetadataOptionsText),
                default);
            await LoadCustomMetadataFieldsAsync(default);
            SelectedCustomMetadataField = CustomMetadataFields.FirstOrDefault(field => field.Id == fieldId);
            CustomMetadataStatusMessage = "CustomMetadataOptionsSaved";
            CustomMetadataFieldsChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    private bool CanDeleteCustomMetadataField() =>
        customMetadataRepository is not null &&
        !IsCustomMetadataBusy &&
        SelectedCustomMetadataField is not null;

    private async Task DeleteCustomMetadataFieldAsync()
    {
        if (customMetadataRepository is null || SelectedCustomMetadataField is null)
        {
            return;
        }

        await ExecuteCustomMetadataOperationAsync(async () =>
        {
            var deleted = SelectedCustomMetadataField;
            await customMetadataRepository.DeleteDefinitionAsync(deleted.Id, default);
            CustomMetadataFields.Remove(deleted);
            SelectedCustomMetadataField = CustomMetadataFields.FirstOrDefault();
            CustomMetadataStatusMessage = "CustomMetadataFieldDeleted";
            CustomMetadataFieldsChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    private async Task ExecuteCustomMetadataOperationAsync(Func<Task> operation)
    {
        IsCustomMetadataBusy = true;
        CustomMetadataStatusMessage = string.Empty;
        try
        {
            await operation();
        }
        catch (ArgumentException exception) when (exception.Message == "CustomMetadataOptionsSemicolonNotAllowed")
        {
            CustomMetadataStatusMessage = "CustomMetadataOptionsSemicolonNotAllowed";
        }
        catch (InvalidOperationException)
        {
            CustomMetadataStatusMessage = "CustomMetadataFieldDuplicate";
        }
        finally
        {
            IsCustomMetadataBusy = false;
        }
    }

    private static IReadOnlyList<string> ParseOptions(string optionsText)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in optionsText
                     .Split(["\r\n", "\n", "\r"], StringSplitOptions.None)
                     .Select(value => value.Trim())
                     .Where(value => value.Length > 0))
        {
            if (option.Contains(';', StringComparison.Ordinal))
            {
                throw new ArgumentException("CustomMetadataOptionsSemicolonNotAllowed");
            }

            if (seen.Add(option))
            {
                result.Add(option);
            }
        }

        return result.AsReadOnly();
    }
}

public sealed class CustomMetadataFieldViewModel(CustomMetadataFieldDefinition definition)
{
    public Guid Id { get; } = definition.Id;
    public string Key { get; } = definition.Key;
    public string Name { get; } = definition.Name;
    public CustomMetadataFieldType Type { get; } = definition.Type;
    public IReadOnlyList<string> Options { get; } = definition.Options;
    public bool CanHaveOptions => Type is CustomMetadataFieldType.SingleSelect or CustomMetadataFieldType.MultiSelect;
}

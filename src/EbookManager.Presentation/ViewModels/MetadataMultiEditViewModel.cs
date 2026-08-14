using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EbookManager.Domain.Books;
using EbookManager.Domain.CustomMetadata;
using System.Collections.ObjectModel;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class MetadataMultiEditViewModel : ObservableObject
{
    public MetadataMultiEditViewModel(
        int selectedBookCount,
        IReadOnlyList<CustomMetadataFieldDefinition>? customMetadataFields = null)
    {
        SelectedBookCount = selectedBookCount;
        foreach (var field in customMetadataFields ?? [])
        {
            var item = new MetadataMultiEditCustomFieldViewModel(field);
            item.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MetadataMultiEditCustomFieldViewModel.UpdateValue))
                {
                    OnChangeSelectionChanged();
                }
            };
            CustomFields.Add(item);
        }
    }

    public int SelectedBookCount { get; }

    public IReadOnlyList<ReadingStatus> AvailableReadingStatuses { get; } = Enum.GetValues<ReadingStatus>();
    public ObservableCollection<MetadataMultiEditCustomFieldViewModel> CustomFields { get; } = [];
    public bool HasCustomFields => CustomFields.Count > 0;

    [ObservableProperty]
    private bool updateAuthors;

    [ObservableProperty]
    private string authorsText = string.Empty;

    [ObservableProperty]
    private bool updateSeries;

    [ObservableProperty]
    private string seriesText = string.Empty;

    [ObservableProperty]
    private bool updateTags;

    [ObservableProperty]
    private string tagsText = string.Empty;

    [ObservableProperty]
    private bool updateLanguage;

    [ObservableProperty]
    private string languageText = string.Empty;

    [ObservableProperty]
    private bool updateStatus;

    [ObservableProperty]
    private ReadingStatus status = ReadingStatus.Unread;

    public bool HasSelectedChanges =>
        UpdateAuthors ||
        UpdateSeries ||
        UpdateTags ||
        UpdateLanguage ||
        UpdateStatus ||
        CustomFields.Any(customField => customField.UpdateValue);

    public MetadataMultiEditResult CreateResult() =>
        new(
            UpdateAuthors,
            AuthorsText,
            UpdateSeries,
            SeriesText,
            UpdateTags,
            TagsText,
            UpdateLanguage,
            LanguageText,
            UpdateStatus,
            Status,
            CustomFields
                .Where(field => field.UpdateValue)
                .Select(field => new MetadataMultiEditCustomFieldResult(
                    field.FieldId,
                    field.Name,
                    field.Type,
                    field.ValueText))
                .ToArray());

    partial void OnUpdateAuthorsChanged(bool value) => OnChangeSelectionChanged();
    partial void OnUpdateSeriesChanged(bool value) => OnChangeSelectionChanged();
    partial void OnUpdateTagsChanged(bool value) => OnChangeSelectionChanged();
    partial void OnUpdateLanguageChanged(bool value) => OnChangeSelectionChanged();
    partial void OnUpdateStatusChanged(bool value) => OnChangeSelectionChanged();

    private void OnChangeSelectionChanged()
    {
        OnPropertyChanged(nameof(HasSelectedChanges));
        ApplyCommand.NotifyCanExecuteChanged();
    }

    public IRelayCommand ApplyCommand => applyCommand ??= new RelayCommand(() => RequestClose?.Invoke(this, true), () => HasSelectedChanges);
    public IRelayCommand CancelCommand => cancelCommand ??= new RelayCommand(() => RequestClose?.Invoke(this, false));

    private RelayCommand? applyCommand;
    private RelayCommand? cancelCommand;

    public event EventHandler<bool>? RequestClose;
}

public sealed record MetadataMultiEditResult(
    bool UpdateAuthors,
    string AuthorsText,
    bool UpdateSeries,
    string SeriesText,
    bool UpdateTags,
    string TagsText,
    bool UpdateLanguage,
    string LanguageText,
    bool UpdateStatus,
    ReadingStatus Status,
    IReadOnlyList<MetadataMultiEditCustomFieldResult>? CustomFields = null);

public sealed record MetadataMultiEditCustomFieldResult(
    Guid FieldId,
    string Name,
    CustomMetadataFieldType Type,
    string? ValueText);

public sealed partial class MetadataMultiEditCustomFieldViewModel : ObservableObject
{
    private bool isSynchronizingMultiSelectOptions;

    public MetadataMultiEditCustomFieldViewModel(CustomMetadataFieldDefinition definition)
    {
        FieldId = definition.Id;
        Name = definition.Name;
        Type = definition.Type;
        Options = definition.Options;
        SingleSelectOptions = new string?[] { null }.Concat(definition.Options).ToArray();
        MultiSelectOptions = new ObservableCollection<CustomMetadataOptionValueViewModel>(
            definition.Options.Select(option => new CustomMetadataOptionValueViewModel(option)));
        foreach (var option in MultiSelectOptions)
        {
            option.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CustomMetadataOptionValueViewModel.IsSelected))
                {
                    UpdateValueTextFromMultiSelectOptions();
                }
            };
        }
    }

    public Guid FieldId { get; }
    public string Name { get; }
    public CustomMetadataFieldType Type { get; }
    public IReadOnlyList<string> Options { get; }
    public IReadOnlyList<string?> SingleSelectOptions { get; }
    public ObservableCollection<CustomMetadataOptionValueViewModel> MultiSelectOptions { get; }
    public bool IsTextEditor => Type == CustomMetadataFieldType.Text;
    public bool IsNumberEditor => Type == CustomMetadataFieldType.Number;
    public bool IsDateEditor => Type == CustomMetadataFieldType.Date;
    public bool IsBooleanEditor => Type == CustomMetadataFieldType.Boolean;
    public bool IsSingleSelectEditor => Type == CustomMetadataFieldType.SingleSelect;
    public bool IsMultiSelectEditor => Type == CustomMetadataFieldType.MultiSelect;

    [ObservableProperty]
    private bool updateValue;

    [ObservableProperty]
    private string? valueText;

    public DateTime? DateValue
    {
        get
        {
            if (DateOnly.TryParse(ValueText, CultureInfo.CurrentCulture, out var date) ||
                DateOnly.TryParseExact(ValueText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return date.ToDateTime(TimeOnly.MinValue);
            }

            return null;
        }
        set => ValueText = value is null
            ? null
            : DateOnly.FromDateTime(value.Value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    partial void OnValueTextChanged(string? value)
    {
        OnPropertyChanged(nameof(DateValue));
        SynchronizeMultiSelectOptionsFromValueText();
    }

    private void SynchronizeMultiSelectOptionsFromValueText()
    {
        if (Type != CustomMetadataFieldType.MultiSelect || isSynchronizingMultiSelectOptions)
        {
            return;
        }

        isSynchronizingMultiSelectOptions = true;
        try
        {
            var selectedValues = ParseMultiSelectValues(ValueText).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var option in MultiSelectOptions)
            {
                option.IsSelected = selectedValues.Contains(option.Value);
            }
        }
        finally
        {
            isSynchronizingMultiSelectOptions = false;
        }
    }

    private void UpdateValueTextFromMultiSelectOptions()
    {
        if (isSynchronizingMultiSelectOptions)
        {
            return;
        }

        ValueText = string.Join("; ", MultiSelectOptions
            .Where(option => option.IsSelected)
            .Select(option => option.Value));
    }

    private static IEnumerable<string> ParseMultiSelectValues(string? value) =>
        CustomMetadataValueParser.SplitList(value, distinct: true);
}

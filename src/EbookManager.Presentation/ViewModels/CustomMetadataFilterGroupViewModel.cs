using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Domain.CustomMetadata;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class CustomMetadataFilterGroupViewModel(
    CustomMetadataFieldDefinition definition,
    ObservableCollection<FacetFilterViewModel> filters)
    : ObservableObject
{
    private const int FilterSearchMinimumItemCount = 8;

    public Guid FieldId => definition.Id;
    public string Name => definition.Name;
    public CustomMetadataFieldType Type => definition.Type;
    public bool CanCleanupValues => Type is
        CustomMetadataFieldType.Text or
        CustomMetadataFieldType.SingleSelect or
        CustomMetadataFieldType.MultiSelect;
    public ObservableCollection<FacetFilterViewModel> Filters { get; } = filters;
    public int VisibleFilterCount => Filters.Count(filter => filter.IsVisible);
    public int TotalFilterCount => Filters.Count;
    public string FilterSearchSummary => $"{VisibleFilterCount} / {TotalFilterCount}";
    public bool HasFilterSearch =>
        TotalFilterCount >= FilterSearchMinimumItemCount || !string.IsNullOrWhiteSpace(FilterSearchText);

    [ObservableProperty]
    private string filterSearchText = string.Empty;

    public void ApplySearch() => ApplySearch(FilterSearchText);

    partial void OnFilterSearchTextChanged(string value) => ApplySearch(value);

    private void ApplySearch(string? searchText)
    {
        var query = searchText?.Trim();
        foreach (var filter in Filters)
        {
            filter.IsVisible = string.IsNullOrWhiteSpace(query) ||
                FilterTextMatches(filter, query);
        }

        OnPropertyChanged(nameof(VisibleFilterCount));
        OnPropertyChanged(nameof(TotalFilterCount));
        OnPropertyChanged(nameof(FilterSearchSummary));
        OnPropertyChanged(nameof(HasFilterSearch));
    }

    private static bool FilterTextMatches(FacetFilterViewModel filter, string query) =>
        filter.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
        filter.DisplayText.Contains(query, StringComparison.CurrentCultureIgnoreCase);
}

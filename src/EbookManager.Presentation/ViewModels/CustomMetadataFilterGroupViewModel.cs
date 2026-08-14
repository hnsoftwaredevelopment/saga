using System.Collections.ObjectModel;
using EbookManager.Domain.CustomMetadata;

namespace EbookManager.Presentation.ViewModels;

public sealed class CustomMetadataFilterGroupViewModel(
    CustomMetadataFieldDefinition definition,
    ObservableCollection<FacetFilterViewModel> filters)
{
    public Guid FieldId => definition.Id;
    public string Name => definition.Name;
    public CustomMetadataFieldType Type => definition.Type;
    public ObservableCollection<FacetFilterViewModel> Filters { get; } = filters;
}

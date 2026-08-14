using EbookManager.Domain.CustomMetadata;

namespace EbookManager.Presentation.ViewModels;

public sealed class LibraryColumnLayoutSnapshot(
    IReadOnlyList<LibraryColumnKey> visibleColumns,
    IReadOnlyDictionary<LibraryColumnKey, double> columnWidths,
    IReadOnlyDictionary<Guid, CustomMetadataFieldDefinition> customMetadataFields)
{
    public IReadOnlyList<LibraryColumnKey> VisibleColumns { get; } = visibleColumns;
    public IReadOnlyDictionary<LibraryColumnKey, double> ColumnWidths { get; } = columnWidths;
    public IReadOnlyDictionary<Guid, CustomMetadataFieldDefinition> CustomMetadataFields { get; } = customMetadataFields;
}

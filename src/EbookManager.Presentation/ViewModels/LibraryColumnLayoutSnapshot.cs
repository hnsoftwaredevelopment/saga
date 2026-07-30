namespace EbookManager.Presentation.ViewModels;

public sealed class LibraryColumnLayoutSnapshot(
    IReadOnlyCollection<LibraryColumnOption> visibleColumns,
    IReadOnlyDictionary<LibraryColumnOption, double> columnWidths)
{
    public IReadOnlyCollection<LibraryColumnOption> VisibleColumns { get; } = visibleColumns;
    public IReadOnlyDictionary<LibraryColumnOption, double> ColumnWidths { get; } = columnWidths;
}

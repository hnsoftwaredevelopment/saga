namespace EbookManager.Presentation.ViewModels;

public sealed class LibraryColumnLayoutSnapshot(
    IReadOnlyList<LibraryColumnOption> visibleColumns,
    IReadOnlyDictionary<LibraryColumnOption, double> columnWidths)
{
    public IReadOnlyList<LibraryColumnOption> VisibleColumns { get; } = visibleColumns;
    public IReadOnlyDictionary<LibraryColumnOption, double> ColumnWidths { get; } = columnWidths;
}

namespace EbookManager.Presentation.ViewModels;

public sealed partial class LibraryViewModel
{
    private readonly BookshelfSelectionState bookshelfSelection = new();
    private int selectionRefreshDepth;
    public bool IsReconcilingBookSelection => selectionRefreshDepth > 0;

    public void SetSelectedBooks(IEnumerable<BookRowViewModel> selectedRows)
    {
        var visible = VisibleBooks.ToDictionary(row => row.Id);
        var rows = selectedRows.Select(row => visible.GetValueOrDefault(row.Id)).OfType<BookRowViewModel>().DistinctBy(row => row.Id).ToArray();
        if (SelectedBooks.SequenceEqual(rows)) return;
        if (!SelectedBooks.Select(row => row.Id).SequenceEqual(rows.Select(row => row.Id))) multiEditSelectionVersion++;
        foreach (var row in SelectedBooks) row.IsSelected = false;
        SelectedBooks.Clear();
        foreach (var row in rows) { row.IsSelected = true; SelectedBooks.Add(row); }
        if (rows.Length == 0) bookshelfSelection.Reset();
        OnPropertyChanged(nameof(SelectedBookCount));
        OnPropertyChanged(nameof(CanMultiEditSelectedBooks));
        OnPropertyChanged(nameof(MetadataMultiEditMenuHeader));
        showMetadataMultiEditCommand?.NotifyCanExecuteChanged();
    }

    public IReadOnlyList<BookRowViewModel> GetBookshelfSelectionOrder() =>
        (IsBookshelfGrouped ? ExpandedBooks(GroupedLibraryNodes) : VisibleBooks).DistinctBy(row => row.Id).ToArray();

    private static IEnumerable<BookRowViewModel> ExpandedBooks(IEnumerable<LibraryGroupNodeViewModel> groups)
    {
        foreach (var group in groups.Where(group => group.IsExpanded))
        {
            foreach (var row in ExpandedBooks(group.Groups)) yield return row;
            foreach (var row in group.Books) yield return row;
        }
    }

    public void SelectBookshelfBook(BookRowViewModel row, bool control, bool shift)
    {
        if (IsCleaningMetadata || multiEditActive) return;
        var order = GetBookshelfSelectionOrder();
        if (!order.Any(item => item.Id == row.Id)) return;
        var ids = bookshelfSelection.Select(order.Select(item => item.Id).ToArray(), SelectedBooks.Select(item => item.Id), row.Id, control, shift).ToHashSet();
        SetSelectedBooks(VisibleBooks.Where(item => ids.Contains(item.Id)));
        SelectedBook = row;
    }
}

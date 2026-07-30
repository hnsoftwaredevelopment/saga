using System.Collections.Specialized;
using EbookManager.Presentation.ViewModels;
using Syncfusion.UI.Xaml.Grid;

namespace EbookManager.App.Views;

internal sealed class LibraryGridColumnVisibility
{
    private readonly SfDataGrid grid;
    private readonly LibraryView view;
    private LibraryViewModel? viewModel;
    private bool isApplyingLayout;

    public LibraryGridColumnVisibility(SfDataGrid grid, LibraryView view)
    {
        this.grid = grid;
        this.view = view;
    }

    public void Attach(LibraryViewModel? newViewModel)
    {
        if (ReferenceEquals(viewModel, newViewModel))
        {
            Apply();
            return;
        }

        Detach();
        viewModel = newViewModel;

        if (viewModel is not null)
        {
            viewModel.ActiveColumnOptions.CollectionChanged += ActiveColumnOptionsChanged;
            grid.ResizingColumns += GridResizingColumns;
        }

        Apply();
    }

    public void Detach()
    {
        if (viewModel is not null)
        {
            viewModel.ActiveColumnOptions.CollectionChanged -= ActiveColumnOptionsChanged;
            grid.ResizingColumns -= GridResizingColumns;
            viewModel = null;
        }
    }

    public void Apply()
    {
        if (viewModel is null)
        {
            return;
        }

        isApplyingLayout = true;
        try
        {
            var orderedVisibleColumns = viewModel.GetVisibleColumns(view);
            var visibleColumns = orderedVisibleColumns.ToHashSet();

            foreach (var column in grid.Columns)
            {
                if (TryGetColumnOption(column.MappingName, out var option))
                {
                    column.IsHidden = !visibleColumns.Contains(option);
                    column.Width = viewModel.GetColumnWidth(view, option, column.Width);
                }
            }

            ApplyColumnOrder(orderedVisibleColumns);
        }
        finally
        {
            isApplyingLayout = false;
        }
    }

    private void ActiveColumnOptionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Apply();
    }

    private void ApplyColumnOrder(IReadOnlyList<LibraryColumnOption> orderedVisibleColumns)
    {
        for (var desiredIndex = 0; desiredIndex < orderedVisibleColumns.Count; desiredIndex++)
        {
            var option = orderedVisibleColumns[desiredIndex];
            var currentIndex = FindColumnIndex(option);
            if (currentIndex < 0 || currentIndex == desiredIndex)
            {
                continue;
            }

            var column = grid.Columns[currentIndex];
            grid.Columns.RemoveAt(currentIndex);
            grid.Columns.Insert(desiredIndex, column);
        }
    }

    private int FindColumnIndex(LibraryColumnOption option)
    {
        for (var index = 0; index < grid.Columns.Count; index++)
        {
            if (TryGetColumnOption(grid.Columns[index].MappingName, out var columnOption) &&
                columnOption == option)
            {
                return index;
            }
        }

        return -1;
    }

    private async void GridResizingColumns(object? sender, ResizingColumnsEventArgs e)
    {
        if (isApplyingLayout ||
            e.Reason != ColumnResizingReason.Resized ||
            viewModel is null ||
            e.ColumnIndex < 0 ||
            e.ColumnIndex >= grid.Columns.Count)
        {
            return;
        }

        var column = grid.Columns[e.ColumnIndex];
        if (!TryGetColumnOption(column.MappingName, out var option))
        {
            return;
        }

        try
        {
            var width = e.Width > 0 ? e.Width : column.ActualWidth;
            await viewModel.SetColumnWidthAsync(view, option, width, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
        }
    }

    private static bool TryGetColumnOption(string mappingName, out LibraryColumnOption option)
    {
        option = mappingName switch
        {
            "CoverBytes" => LibraryColumnOption.Cover,
            "Title" => LibraryColumnOption.Title,
            "Authors" => LibraryColumnOption.Authors,
            "Formats" => LibraryColumnOption.Format,
            "Series" => LibraryColumnOption.Series,
            "SeriesNumber" => LibraryColumnOption.SeriesNumber,
            "ReadingStatus" => LibraryColumnOption.Status,
            "Language" => LibraryColumnOption.Language,
            "Publisher" => LibraryColumnOption.Publisher,
            "PublicationDateSortValue" => LibraryColumnOption.PublicationDate,
            "Tags" => LibraryColumnOption.Tags,
            "Isbn" => LibraryColumnOption.Isbn,
            "Description" => LibraryColumnOption.Description,
            "DateAddedSortValue" => LibraryColumnOption.DateAdded,
            "LastModifiedSortValue" => LibraryColumnOption.LastModified,
            "EReader" => LibraryColumnOption.EReader,
            _ => default,
        };

        return mappingName is
            "CoverBytes" or
            "Title" or
            "Authors" or
            "Formats" or
            "Series" or
            "SeriesNumber" or
            "ReadingStatus" or
            "Language" or
            "Publisher" or
            "PublicationDateSortValue" or
            "Tags" or
            "Isbn" or
            "Description" or
            "DateAddedSortValue" or
            "LastModifiedSortValue" or
            "EReader";
    }
}

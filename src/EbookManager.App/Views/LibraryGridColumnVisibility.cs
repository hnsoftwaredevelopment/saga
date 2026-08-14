using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using EbookManager.App.Controls;
using EbookManager.App.Converters;
using EbookManager.Presentation.ViewModels;
using Syncfusion.UI.Xaml.Grid;

namespace EbookManager.App.Views;

internal sealed class LibraryGridColumnVisibility
{
    private readonly SfDataGrid grid;
    private readonly LibraryView view;
    private const string CustomMappingPrefix = "CustomMetadata:";
    private static readonly CustomMetadataValueConverter CustomMetadataValueConverter = new();
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
            viewModel.PropertyChanged += ViewModelPropertyChanged;
            grid.ResizingColumns += GridResizingColumns;
        }

        Apply();
    }

    public void Detach()
    {
        if (viewModel is not null)
        {
            viewModel.ActiveColumnOptions.CollectionChanged -= ActiveColumnOptionsChanged;
            viewModel.PropertyChanged -= ViewModelPropertyChanged;
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
            EnsureCustomColumns(orderedVisibleColumns);

            foreach (var column in grid.Columns)
            {
                if (TryGetColumnKey(column.MappingName, out var option))
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

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LibraryViewModel.ActiveColumnLayoutSnapshot))
        {
            Apply();
        }
    }

    private void ApplyColumnOrder(IReadOnlyList<LibraryColumnKey> orderedVisibleColumns)
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

    private int FindColumnIndex(LibraryColumnKey option)
    {
        for (var index = 0; index < grid.Columns.Count; index++)
        {
            if (TryGetColumnKey(grid.Columns[index].MappingName, out var columnOption) &&
                columnOption == option)
            {
                return index;
            }
        }

        return -1;
    }

    private void EnsureCustomColumns(IReadOnlyList<LibraryColumnKey> orderedVisibleColumns)
    {
        var desiredCustomColumns = orderedVisibleColumns
            .Where(column => column.CustomFieldId is not null)
            .ToHashSet();

        for (var index = grid.Columns.Count - 1; index >= 0; index--)
        {
            var column = grid.Columns[index];
            if (TryGetCustomColumnKey(column.MappingName, out var key) &&
                !desiredCustomColumns.Contains(key))
            {
                grid.Columns.RemoveAt(index);
            }
        }

        if (viewModel is null)
        {
            return;
        }

        foreach (var key in desiredCustomColumns)
        {
            var existingIndex = FindColumnIndex(key);
            if (existingIndex >= 0)
            {
                grid.Columns[existingIndex].HeaderText = viewModel.GetColumnHeaderText(key);
                continue;
            }

            var fieldId = key.CustomFieldId!.Value;
            grid.Columns.Add(new GridTemplateColumn
            {
                HeaderText = viewModel.GetColumnHeaderText(key),
                MappingName = GetCustomMappingName(fieldId),
                AllowSorting = false,
                Width = viewModel.GetColumnWidth(view, key, 160),
                CellTemplate = CreateCustomMetadataCellTemplate(fieldId)
            });
        }
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
        if (!TryGetColumnKey(column.MappingName, out var option))
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

    private static DataTemplate CreateCustomMetadataCellTemplate(Guid fieldId)
    {
        var text = new FrameworkElementFactory(typeof(HighlightedTextBlock));
        text.SetResourceReference(HighlightedTextBlock.HighlightBrushProperty, "SearchHighlightBrush");
        text.SetResourceReference(HighlightedTextBlock.ForegroundProperty, "TextSecondaryBrush");
        text.SetBinding(
            HighlightedTextBlock.HighlightedTextProperty,
            new Binding
            {
                Converter = CustomMetadataValueConverter,
                ConverterParameter = fieldId.ToString("D")
            });
        text.SetBinding(
            HighlightedTextBlock.SearchTextProperty,
            new Binding(nameof(BookRowViewModel.SearchText)));
        return new DataTemplate { VisualTree = text };
    }

    private static bool TryGetColumnKey(string mappingName, out LibraryColumnKey option)
    {
        if (TryGetCustomColumnKey(mappingName, out option))
        {
            return true;
        }

        var standardOption = mappingName switch
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
            _ => (LibraryColumnOption?)null,
        };

        option = standardOption is null
            ? new LibraryColumnKey(mappingName)
            : LibraryColumnKey.FromStandard(standardOption.Value);
        return standardOption is not null;
    }

    private static bool TryGetCustomColumnKey(string mappingName, out LibraryColumnKey option)
    {
        if (mappingName.StartsWith(CustomMappingPrefix, StringComparison.OrdinalIgnoreCase) &&
            Guid.TryParse(mappingName[CustomMappingPrefix.Length..], out var fieldId))
        {
            option = LibraryColumnKey.FromCustom(fieldId);
            return true;
        }

        option = new LibraryColumnKey(mappingName);
        return false;
    }

    private static string GetCustomMappingName(Guid fieldId) => $"{CustomMappingPrefix}{fieldId:D}";
}

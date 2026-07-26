using System.Collections.Specialized;
using EbookManager.Presentation.ViewModels;
using Syncfusion.UI.Xaml.Grid;

namespace EbookManager.App.Views;

internal sealed class LibraryGridColumnVisibility
{
    private readonly SfDataGrid grid;
    private readonly LibraryView view;
    private LibraryViewModel? viewModel;

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
        }

        Apply();
    }

    public void Detach()
    {
        if (viewModel is not null)
        {
            viewModel.ActiveColumnOptions.CollectionChanged -= ActiveColumnOptionsChanged;
            viewModel = null;
        }
    }

    public void Apply()
    {
        if (viewModel is null)
        {
            return;
        }

        var visibleColumns = viewModel.GetVisibleColumns(view).ToHashSet();

        foreach (var column in grid.Columns)
        {
            if (TryGetColumnOption(column.MappingName, out var option))
            {
                column.IsHidden = !visibleColumns.Contains(option);
            }
        }
    }

    private void ActiveColumnOptionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Apply();
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

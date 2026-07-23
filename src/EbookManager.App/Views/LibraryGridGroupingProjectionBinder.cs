using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using EbookManager.App.Localization;
using EbookManager.Presentation.ViewModels;
using Syncfusion.Data;
using Syncfusion.UI.Xaml.Grid;

namespace EbookManager.App.Views;

internal sealed class LibraryGridGroupingProjectionBinder
{
    private readonly SfDataGrid grid;
    private readonly ObservableCollection<BookRowViewModel> projectedRows = [];
    private LibraryViewModel? viewModel;
    private bool isRefreshing;

    public LibraryGridGroupingProjectionBinder(FrameworkElement owner, SfDataGrid grid)
    {
        this.grid = grid;
        this.grid.GroupColumnDescriptions.CollectionChanged += OnGroupColumnDescriptionsChanged;

        owner.DataContextChanged += OnDataContextChanged;
        LocalizedStrings.Current.PropertyChanged += OnLocalizedStringsChanged;

        Attach(owner.DataContext as LibraryViewModel);
        RefreshCaptionSummary();
        RefreshProjection();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        Detach();
        Attach(e.NewValue as LibraryViewModel);
        RefreshProjection();
    }

    private void Attach(LibraryViewModel? newViewModel)
    {
        viewModel = newViewModel;
        if (viewModel is null)
        {
            return;
        }

        viewModel.VisibleBooks.CollectionChanged += OnVisibleBooksChanged;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void Detach()
    {
        if (viewModel is null)
        {
            return;
        }

        viewModel.VisibleBooks.CollectionChanged -= OnVisibleBooksChanged;
        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        viewModel = null;
    }

    private void OnVisibleBooksChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshProjection();

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LibraryViewModel.VisibleBooks))
        {
            RefreshProjection();
        }
    }

    private void OnGroupColumnDescriptionsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshProjection();

    private void OnLocalizedStringsChanged(object? sender, PropertyChangedEventArgs e)
    {
        RefreshCaptionSummary();
        grid.View?.Refresh();
    }

    private void RefreshProjection()
    {
        if (isRefreshing)
        {
            return;
        }

        isRefreshing = true;
        try
        {
            projectedRows.Clear();
            if (viewModel is null)
            {
                grid.ItemsSource = null;
                return;
            }

            var groupColumns = grid.GroupColumnDescriptions
                .Select(description => description.ColumnName)
                .Where(columnName => !string.IsNullOrWhiteSpace(columnName))
                .ToList();

            if (!LibraryGridRowProjector.RequiresProjection(groupColumns))
            {
                projectedRows.Clear();
                grid.ItemsSource = viewModel.VisibleBooks;
                return;
            }

            if (!ReferenceEquals(grid.ItemsSource, projectedRows))
            {
                grid.ItemsSource = projectedRows;
            }

            foreach (var row in LibraryGridRowProjector.Project(viewModel.VisibleBooks, groupColumns))
            {
                projectedRows.Add(row);
            }
        }
        finally
        {
            isRefreshing = false;
        }
    }

    private void RefreshCaptionSummary()
    {
        grid.GroupCaptionTextFormat = "{Key}";
        grid.CaptionSummaryRow = new GridSummaryRow
        {
            ShowSummaryInRow = true,
            Title = "{Key} - {BookCount}",
            SummaryColumns =
            [
                new GridSummaryColumn
                {
                    Name = "BookCount",
                    MappingName = nameof(BookRowViewModel.Id),
                    SummaryType = SummaryType.Custom,
                    CustomAggregate = new LocalizedBookCountAggregate(),
                    Format = "{Text}"
                }
            ]
        };
    }
}

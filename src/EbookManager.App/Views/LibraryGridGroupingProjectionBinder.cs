using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using EbookManager.Presentation.ViewModels;
using Syncfusion.UI.Xaml.Grid;

namespace EbookManager.App.Views;

internal sealed class LibraryGridGroupingProjectionBinder
{
    private readonly SfDataGrid grid;
    private readonly ObservableCollection<BookRowViewModel> projectedRows = [];
    private LibraryViewModel? viewModel;
    private bool isRefreshing;
    private bool isRefreshQueued;
    private string projectionSignature = string.Empty;

    public LibraryGridGroupingProjectionBinder(FrameworkElement owner, SfDataGrid grid)
    {
        this.grid = grid;
        this.grid.GroupColumnDescriptions.CollectionChanged += OnGroupColumnDescriptionsChanged;

        owner.DataContextChanged += OnDataContextChanged;

        Attach(owner.DataContext as LibraryViewModel);
        RefreshCaptionSummary();
        QueueRefreshProjection();
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

    private void OnVisibleBooksChanged(object? sender, NotifyCollectionChangedEventArgs e) => QueueRefreshProjection();

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LibraryViewModel.VisibleBooks))
        {
            QueueRefreshProjection();
        }
    }

    private void OnGroupColumnDescriptionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (ProjectionSignatureChanged())
        {
            QueueRefreshProjection();
        }
    }

    private void QueueRefreshProjection()
    {
        if (isRefreshQueued)
        {
            return;
        }

        isRefreshQueued = true;
        grid.Dispatcher.BeginInvoke(
            new Action(() =>
            {
                isRefreshQueued = false;
                RefreshProjection();
            }),
            DispatcherPriority.Background);
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
            projectionSignature = CreateProjectionSignature(groupColumns);

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
        grid.CaptionSummaryRow = null;
    }

    private bool ProjectionSignatureChanged()
    {
        var groupColumns = grid.GroupColumnDescriptions
            .Select(description => description.ColumnName)
            .Where(columnName => !string.IsNullOrWhiteSpace(columnName));
        return !StringComparer.Ordinal.Equals(projectionSignature, CreateProjectionSignature(groupColumns));
    }

    private static string CreateProjectionSignature(IEnumerable<string> groupColumnNames) =>
        string.Join("|", LibraryGridRowProjector.GetActiveProjectionColumns(groupColumnNames));
}

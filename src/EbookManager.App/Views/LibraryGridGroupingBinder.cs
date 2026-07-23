using System.ComponentModel;
using System.Windows;
using EbookManager.Presentation.ViewModels;
using Syncfusion.UI.Xaml.Grid;

namespace EbookManager.App.Views;

internal sealed class LibraryGridGroupingBinder
{
    private readonly FrameworkElement owner;
    private readonly SfDataGrid grid;
    private LibraryViewModel? viewModel;

    public LibraryGridGroupingBinder(FrameworkElement owner, SfDataGrid grid)
    {
        this.owner = owner;
        this.grid = grid;
        owner.DataContextChanged += OnDataContextChanged;
        ApplyGrouping();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (viewModel is not null)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        viewModel = e.NewValue as LibraryViewModel;
        if (viewModel is not null)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        ApplyGrouping();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LibraryViewModel.SelectedGroupOption)
            or nameof(LibraryViewModel.SecondaryGroupOption))
        {
            ApplyGrouping();
        }
    }

    private void ApplyGrouping()
    {
        grid.GroupColumnDescriptions.Clear();
        if (viewModel is null)
        {
            return;
        }

        var hasPrimary = viewModel.SelectedGroupOption != LibraryGroupOption.None;
        var hasSecondary = viewModel.SecondaryGroupOption != LibraryGroupOption.None &&
            viewModel.SecondaryGroupOption != viewModel.SelectedGroupOption;
        if (hasPrimary || hasSecondary)
        {
            grid.GroupColumnDescriptions.Add(new GroupColumnDescription
            {
                ColumnName = nameof(BookRowViewModel.PrimaryGroupName)
            });
        }

        if (hasPrimary && hasSecondary)
        {
            grid.GroupColumnDescriptions.Add(new GroupColumnDescription
            {
                ColumnName = nameof(BookRowViewModel.SecondaryGroupName)
            });
        }
    }
}

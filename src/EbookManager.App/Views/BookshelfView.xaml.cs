using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using EbookManager.App.Controls;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class BookshelfView : UserControl
{
    private LibraryViewModel? attachedViewModel;
    private bool isLayoutRefreshQueued;

    public BookshelfView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            AttachViewModel(DataContext as LibraryViewModel);
            QueueBookshelfLayoutRefresh();
        };
        Unloaded += (_, _) => AttachViewModel(null);
        IsVisibleChanged += (_, _) => QueueBookshelfLayoutRefresh();
        SizeChanged += (_, _) => QueueBookshelfLayoutRefresh();
        DataContextChanged += (_, e) => AttachViewModel(e.NewValue as LibraryViewModel);
    }

    private void BookRowMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is LibraryViewModel viewModel &&
            sender is FrameworkElement { DataContext: BookRowViewModel row })
        {
            viewModel.SelectedBook = row;
        }
    }

    private void GroupExpanderExpanded(object sender, RoutedEventArgs e)
    {
        QueueBookshelfLayoutRefresh();
    }

    private void AttachViewModel(LibraryViewModel? viewModel)
    {
        if (ReferenceEquals(attachedViewModel, viewModel))
        {
            return;
        }

        if (attachedViewModel is not null)
        {
            attachedViewModel.PropertyChanged -= ViewModelPropertyChanged;
        }

        attachedViewModel = viewModel;
        if (attachedViewModel is not null)
        {
            attachedViewModel.PropertyChanged += ViewModelPropertyChanged;
        }

        QueueBookshelfLayoutRefresh();
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LibraryViewModel.BookshelfVisibleBooksSource)
            or nameof(LibraryViewModel.BookshelfGroupedLibraryNodesSource)
            or nameof(LibraryViewModel.IsBookshelfGrouped)
            or nameof(LibraryViewModel.SelectedView)
            or nameof(LibraryViewModel.IsLoadingLibrary))
        {
            QueueBookshelfLayoutRefresh();
        }
    }

    private void QueueBookshelfLayoutRefresh()
    {
        if (isLayoutRefreshQueued)
        {
            return;
        }

        isLayoutRefreshQueued = true;
        Dispatcher.BeginInvoke(
            () =>
            {
                isLayoutRefreshQueued = false;
                RefreshBookshelfLayout();
            },
            DispatcherPriority.Loaded);
    }

    private void RefreshBookshelfLayout()
    {
        if (!IsVisible)
        {
            return;
        }

        InvalidateBookshelfLayout(BookshelfBooksList);
        InvalidateBookshelfLayout(BookshelfGroupsList);
    }

    private static void InvalidateBookshelfLayout(DependencyObject root)
    {
        if (root is VirtualizingWrapPanel or WrapPanel)
        {
            if (root is UIElement element)
            {
                element.InvalidateMeasure();
                element.InvalidateArrange();
            }
        }

        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < childCount; index++)
        {
            InvalidateBookshelfLayout(VisualTreeHelper.GetChild(root, index));
        }
    }
}

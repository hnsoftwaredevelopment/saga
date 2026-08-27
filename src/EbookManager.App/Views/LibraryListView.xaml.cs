using System.ComponentModel;
using System.Windows.Threading;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class LibraryListView : System.Windows.Controls.UserControl
{
    private readonly LibraryGridColumnVisibility columnVisibility;
    private LibraryViewModel? attachedViewModel;

    public LibraryListView()
    {
        InitializeComponent();
        columnVisibility = new LibraryGridColumnVisibility(BooksGrid, EbookManager.Presentation.ViewModels.LibraryView.List);
        DataContextChanged += (_, e) => AttachViewModel(e.NewValue as LibraryViewModel);
        Loaded += (_, _) => AttachViewModel(DataContext as LibraryViewModel);
        Unloaded += (_, _) => AttachViewModel(null);
    }

    private void BookRowMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is EbookManager.Presentation.ViewModels.LibraryViewModel viewModel &&
            sender is System.Windows.FrameworkElement { DataContext: EbookManager.Presentation.ViewModels.BookRowViewModel row })
        {
            viewModel.SelectedBook = row;
            viewModel.SetSelectedBooks([row]);
        }
    }

    private void BooksGridPreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is EbookManager.Presentation.ViewModels.LibraryViewModel viewModel)
        {
            LibraryGridSelectionHelper.SelectRowUnderPointer(BooksGrid, viewModel, e);
        }
    }

    private void BooksGridSelectionChanged(object sender, Syncfusion.UI.Xaml.Grid.GridSelectionChangedEventArgs e)
    {
        if (DataContext is EbookManager.Presentation.ViewModels.LibraryViewModel viewModel)
        {
            viewModel.SetSelectedBooks(BooksGrid.SelectedItems.OfType<EbookManager.Presentation.ViewModels.BookRowViewModel>());
        }
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
        columnVisibility.Attach(viewModel);
        if (attachedViewModel is not null)
        {
            attachedViewModel.PropertyChanged += ViewModelPropertyChanged;
        }
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LibraryViewModel.BookRevealRequest) &&
            attachedViewModel is { SelectedView: LibraryView.List, BookRevealRequest: { } request } viewModel)
        {
            Dispatcher.BeginInvoke(
                () => RevealBook(viewModel, request.BookId),
                DispatcherPriority.Loaded);
        }
    }

    private void RevealBook(LibraryViewModel viewModel, Guid bookId)
    {
        if (!IsVisible)
        {
            return;
        }

        if (viewModel.IsLibraryGrouped)
        {
            BookRevealScrollHelper.ScrollListToBook(ListGroupsList, viewModel, bookId, grouped: true);
        }
        else
        {
            BookRevealScrollHelper.ScrollGridToBook(BooksGrid, viewModel, bookId);
        }
    }
}

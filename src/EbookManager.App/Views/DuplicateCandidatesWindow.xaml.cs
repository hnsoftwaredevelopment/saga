using EbookManager.Presentation.ViewModels;
using EbookManager.App.Localization;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Settings;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace EbookManager.App.Views;

public partial class DuplicateCandidatesWindow : System.Windows.Window
{
    private static readonly TimeSpan MergeSuccessMessageDuration = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan ColumnWidthSaveDelay = TimeSpan.FromMilliseconds(700);
    private readonly IAppSettingsStore settingsStore;
    private readonly DispatcherTimer mergeSuccessMessageTimer;
    private readonly DispatcherTimer columnWidthSaveTimer;
    private readonly EventHandler columnWidthChangedHandler;
    private Task columnWidthsLoadedTask = Task.CompletedTask;
    private bool isMergingCandidate;
    private bool isApplyingColumnWidths;

    public DuplicateCandidatesWindow(DuplicateCandidatesViewModel viewModel, IAppSettingsStore settingsStore)
    {
        InitializeComponent();
        DataContext = viewModel;
        this.settingsStore = settingsStore;
        columnWidthChangedHandler = (_, _) => ScheduleColumnWidthSave();
        mergeSuccessMessageTimer = new DispatcherTimer
        {
            Interval = MergeSuccessMessageDuration
        };
        columnWidthSaveTimer = new DispatcherTimer
        {
            Interval = ColumnWidthSaveDelay
        };
        mergeSuccessMessageTimer.Tick += MergeSuccessMessageTimerTick;
        columnWidthSaveTimer.Tick += ColumnWidthSaveTimerTick;
        viewModel.PropertyChanged += ViewModelPropertyChanged;
        Loaded += DuplicateCandidatesWindowLoaded;
        Closed += DuplicateCandidatesWindowClosed;
    }

    private async void DuplicateCandidatesWindowLoaded(object sender, RoutedEventArgs e)
    {
        AttachColumnWidthTracking();
        columnWidthsLoadedTask = ApplyColumnWidthsAsync(CancellationToken.None);
        await IgnoreColumnWidthSettingsFailureAsync(columnWidthsLoadedTask);
        QueueDuplicateGridLayoutRefresh();
    }

    private void CloseClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        Close();
    }

    private void DuplicateRowDoubleClicked(object sender, MouseButtonEventArgs e)
    {
        if (IsInsideButton(e.OriginalSource as System.Windows.DependencyObject))
        {
            e.Handled = true;
            return;
        }

        if (sender is DataGridRow { DataContext: DuplicateCandidateRowViewModel row })
        {
            e.Handled = true;
            ShowDetails(row);
        }
    }

    private void ShowDetailsClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is Button { DataContext: DuplicateCandidateRowViewModel row })
        {
            e.Handled = true;
            ShowDetails(row);
        }
    }

    private void ShowDetails(DuplicateCandidateRowViewModel row)
    {
        var window = new DuplicateCandidateDetailsWindow(row)
        {
            Owner = this
        };
        window.ShowDialog();
    }

    private async void DeleteCandidateButtonClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is Button { DataContext: DuplicateCandidateRowViewModel row })
        {
            e.Handled = true;
            await DeleteCandidateAsync(row);
        }
    }

    private async void IgnoreCandidateButtonClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is Button { DataContext: DuplicateCandidateRowViewModel row })
        {
            e.Handled = true;
            await IgnoreCandidateAsync(row);
        }
    }

    private async void MergeCandidateButtonClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is Button { DataContext: DuplicateCandidateRowViewModel row } button)
        {
            e.Handled = true;
            if (isMergingCandidate)
            {
                return;
            }

            isMergingCandidate = true;
            button.IsEnabled = false;
            try
            {
                await MergeCandidateAsync(row);
            }
            finally
            {
                isMergingCandidate = false;
                button.IsEnabled = true;
            }
        }
    }

    private async void DeleteCandidateClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DuplicateRowsGrid.SelectedItem is not DuplicateCandidateRowViewModel row)
        {
            return;
        }

        e.Handled = true;
        await DeleteCandidateAsync(row);
    }

    private async void IgnoreCandidateClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DuplicateRowsGrid.SelectedItem is not DuplicateCandidateRowViewModel row)
        {
            return;
        }

        e.Handled = true;
        await IgnoreCandidateAsync(row);
    }

    private async Task DeleteCandidateAsync(DuplicateCandidateRowViewModel row)
    {
        if (DataContext is not DuplicateCandidatesViewModel viewModel)
        {
            return;
        }

        await viewModel.DeleteCandidateAsync(row, CancellationToken.None);
        if (!viewModel.HasGroups)
        {
            Close();
        }
    }

    private async Task IgnoreCandidateAsync(DuplicateCandidateRowViewModel row)
    {
        if (DataContext is not DuplicateCandidatesViewModel viewModel)
        {
            return;
        }

        await viewModel.IgnoreCandidateAsync(row, CancellationToken.None);
        if (!viewModel.HasGroups)
        {
            Close();
        }
    }

    private async Task MergeCandidateAsync(DuplicateCandidateRowViewModel row)
    {
        if (DataContext is not DuplicateCandidatesViewModel viewModel)
        {
            return;
        }

        var preview = viewModel.CreateMergePreview(row);
        if (preview is null)
        {
            return;
        }

        var previewWindow = new DuplicateMergePreviewWindow(preview)
        {
            Owner = this
        };
        if (previewWindow.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await viewModel.MergeCandidateAsync(preview, CancellationToken.None);
            if (!viewModel.HasGroups)
            {
                Close();
            }
        }
        catch (Exception)
        {
            System.Windows.MessageBox.Show(
                this,
                LocalizedStrings.Current["DuplicateMergeFailedMessage"],
                LocalizedStrings.Current["DuplicateMergeFailedTitle"],
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            Close();
        }
    }

    private static bool IsInsideButton(System.Windows.DependencyObject? source)
    {
        for (var current = source; current is not null; current = GetParent(current))
        {
            if (current is ButtonBase)
            {
                return true;
            }

            if (current is DataGridRow)
            {
                return false;
            }
        }

        return false;
    }

    private static DependencyObject? GetParent(DependencyObject current) =>
        current is Visual or Visual3D
            ? VisualTreeHelper.GetParent(current)
            : LogicalTreeHelper.GetParent(current);

    private void DuplicateRowsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is DuplicateCandidatesViewModel viewModel)
        {
            viewModel.SetSelectedRows(DuplicateRowsGrid.SelectedItems.OfType<DuplicateCandidateRowViewModel>());
        }
    }

    private void ViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(DuplicateCandidatesViewModel.HasMergeSuccessMessage) ||
            sender is not DuplicateCandidatesViewModel viewModel)
        {
            return;
        }

        mergeSuccessMessageTimer.Stop();
        if (viewModel.HasMergeSuccessMessage)
        {
            mergeSuccessMessageTimer.Start();
        }
    }

    private void MergeSuccessMessageTimerTick(object? sender, EventArgs e)
    {
        mergeSuccessMessageTimer.Stop();
        if (DataContext is DuplicateCandidatesViewModel viewModel)
        {
            viewModel.ClearMergeSuccessMessage();
        }
    }

    private async void ColumnWidthSaveTimerTick(object? sender, EventArgs e)
    {
        columnWidthSaveTimer.Stop();
        await SaveColumnWidthsBestEffortAsync(CancellationToken.None);
    }

    private async void DuplicateCandidatesWindowClosed(object? sender, EventArgs e)
    {
        Loaded -= DuplicateCandidatesWindowLoaded;
        mergeSuccessMessageTimer.Stop();
        columnWidthSaveTimer.Stop();
        mergeSuccessMessageTimer.Tick -= MergeSuccessMessageTimerTick;
        columnWidthSaveTimer.Tick -= ColumnWidthSaveTimerTick;
        await IgnoreColumnWidthSettingsFailureAsync(columnWidthsLoadedTask);
        DetachColumnWidthTracking();
        if (DataContext is DuplicateCandidatesViewModel viewModel)
        {
            viewModel.PropertyChanged -= ViewModelPropertyChanged;
        }

        await SaveColumnWidthsBestEffortAsync(CancellationToken.None);
    }

    private void AttachColumnWidthTracking()
    {
        var descriptor = DependencyPropertyDescriptor.FromProperty(
            DataGridColumn.WidthProperty,
            typeof(DataGridColumn));
        if (descriptor is null)
        {
            return;
        }

        foreach (var column in DuplicateRowsGrid.Columns)
        {
            descriptor.RemoveValueChanged(column, columnWidthChangedHandler);
            descriptor.AddValueChanged(column, columnWidthChangedHandler);
        }
    }

    private void DetachColumnWidthTracking()
    {
        var descriptor = DependencyPropertyDescriptor.FromProperty(
            DataGridColumn.WidthProperty,
            typeof(DataGridColumn));
        if (descriptor is null)
        {
            return;
        }

        foreach (var column in DuplicateRowsGrid.Columns)
        {
            descriptor.RemoveValueChanged(column, columnWidthChangedHandler);
        }
    }

    private void ScheduleColumnWidthSave()
    {
        if (isApplyingColumnWidths)
        {
            return;
        }

        columnWidthSaveTimer.Stop();
        columnWidthSaveTimer.Start();
    }

    private async Task ApplyColumnWidthsAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsStore.LoadAsync(cancellationToken);
        var widths = settings.LibraryColumnWidths?.DuplicateCandidates;
        if (widths is null || widths.Count == 0)
        {
            return;
        }

        isApplyingColumnWidths = true;
        try
        {
            foreach (var column in DuplicateRowsGrid.Columns)
            {
                var key = GetColumnKey(column);
                if (key is not null &&
                    widths.TryGetValue(key, out var width) &&
                    IsUsableColumnWidth(width))
                {
                    column.Width = new DataGridLength(width);
                }
            }
        }
        finally
        {
            isApplyingColumnWidths = false;
        }
    }

    private void QueueDuplicateGridLayoutRefresh()
    {
        Dispatcher.BeginInvoke(
            () =>
            {
                DuplicateRowsGrid.Items.Refresh();
                DuplicateRowsGrid.InvalidateMeasure();
                DuplicateRowsGrid.InvalidateArrange();
                DuplicateRowsGrid.UpdateLayout();
            },
            DispatcherPriority.ContextIdle);
    }

    private async Task SaveColumnWidthsAsync(CancellationToken cancellationToken)
    {
        var widths = CaptureColumnWidths();
        if (widths.Count == 0)
        {
            return;
        }

        var settings = await settingsStore.LoadAsync(cancellationToken);
        await settingsStore.SaveAsync(
            settings with
            {
                LibraryColumnWidths = new LibraryColumnWidthSettings(
                    settings.LibraryColumnWidths?.Detailed,
                    settings.LibraryColumnWidths?.List,
                    widths)
            },
            cancellationToken);
    }

    private async Task SaveColumnWidthsBestEffortAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SaveColumnWidthsAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
        }
    }

    private static async Task IgnoreColumnWidthSettingsFailureAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
        }
    }

    private IReadOnlyDictionary<string, double> CaptureColumnWidths()
    {
        var widths = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var column in DuplicateRowsGrid.Columns)
        {
            var key = GetColumnKey(column);
            var width = column.ActualWidth > 0 ? column.ActualWidth : column.Width.DisplayValue;
            if (key is not null && IsUsableColumnWidth(width))
            {
                widths[key] = Math.Round(width, 2);
            }
        }

        return widths;
    }

    private static string? GetColumnKey(DataGridColumn column) =>
        string.IsNullOrWhiteSpace(column.SortMemberPath)
            ? null
            : column.SortMemberPath;

    private static bool IsUsableColumnWidth(double width) =>
        double.IsFinite(width) && width >= 24 && width <= 2000;
}

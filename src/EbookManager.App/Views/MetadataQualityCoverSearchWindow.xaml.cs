using EbookManager.Presentation.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace EbookManager.App.Views;

public partial class MetadataQualityCoverSearchWindow : Window
{
    private readonly CancellationTokenSource lifetimeCancellation;

    public MetadataQualityCoverSearchWindow(
        MetadataQualityCoverSearchViewModel viewModel,
        CancellationToken cancellationToken)
    {
        InitializeComponent();
        DataContext = viewModel;
        lifetimeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    private async void WindowLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await ((MetadataQualityCoverSearchViewModel)DataContext)
                .LoadAsync(lifetimeCancellation.Token);
            CoverCandidates.Focus();
        }
        catch (OperationCanceledException) when (lifetimeCancellation.IsCancellationRequested)
        {
            if (IsVisible)
            {
                Close();
            }
        }
    }

    private void WindowClosed(object? sender, EventArgs e)
    {
        lifetimeCancellation.Cancel();
        lifetimeCancellation.Dispose();
    }

    private void WindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter &&
            DataContext is MetadataQualityCoverSearchViewModel { CanUseCover: true })
        {
            DialogResult = true;
            e.Handled = true;
        }
    }

    private void CoverCandidateDoubleClicked(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MetadataQualityCoverSearchViewModel { CanUseCover: true })
        {
            DialogResult = true;
        }
    }

    private void UseCoverClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MetadataQualityCoverSearchViewModel { CanUseCover: true })
        {
            DialogResult = true;
        }
    }
}

using EbookManager.App.Services;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class SettingsWindow : System.Windows.Window
{
    private const string ColumnChoiceDragFormat = "Saga.LibraryColumnChoice";
    private readonly SettingsViewModel viewModel;
    private readonly LibraryViewModel libraryViewModel;
    private readonly LocalizationService localizationService;
    private readonly ThemeService themeService;
    private bool isLoadingSettings;
    private string originalTheme = "Light";
    private string originalCulture = "en-US";
    private LibraryView originalSelectedView;

    public SettingsWindow(
        SettingsViewModel viewModel,
        LibraryViewModel libraryViewModel,
        LocalizationService localizationService,
        ThemeService themeService)
    {
        this.viewModel = viewModel;
        this.libraryViewModel = libraryViewModel;
        this.localizationService = localizationService;
        this.themeService = themeService;
        InitializeComponent();
        DataContext = viewModel;
        Loaded += OnLoaded;
    }

    public LibraryViewModel LibraryViewModel => libraryViewModel;

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        isLoadingSettings = true;
        originalCulture = System.Globalization.CultureInfo.CurrentUICulture.Name;
        originalSelectedView = libraryViewModel.SelectedView;
        try
        {
            await viewModel.LoadAsync();
            originalTheme = viewModel.Theme;
            localizationService.ApplyCulture(viewModel.Culture);
        }
        finally
        {
            isLoadingSettings = false;
        }
    }

    private void CultureSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!IsLoaded || isLoadingSettings)
        {
            return;
        }

        localizationService.ApplyCulture(viewModel.Culture);
        libraryViewModel.RefreshLocalizedFilterDisplayNames();
    }

    private void ThemeSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!IsLoaded || isLoadingSettings)
        {
            return;
        }

        themeService.ApplyTheme(viewModel.Theme);
    }

    private void DefaultViewSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!IsLoaded || isLoadingSettings)
        {
            return;
        }

        libraryViewModel.ApplyDefaultViewPreference(viewModel.DefaultView);
    }

    private async void SaveClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.SaveAsync();
        localizationService.ApplyCulture(viewModel.Culture);
        await libraryViewModel.RefreshSettingsDependentDisplayAsync();
        themeService.ApplyTheme(viewModel.Theme);
        DialogResult = true;
    }

    private void CancelClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        localizationService.ApplyCulture(originalCulture);
        libraryViewModel.RefreshLocalizedFilterDisplayNames();
        themeService.ApplyTheme(originalTheme);
        libraryViewModel.SelectedView = originalSelectedView;
        DialogResult = false;
    }

    private async void NormalizeLanguageMetadataClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        var previousCursor = Cursor;
        Cursor = System.Windows.Input.Cursors.Wait;
        if (sender is System.Windows.Controls.Control control)
        {
            control.IsEnabled = false;
        }

        try
        {
            await libraryViewModel.NormalizeLanguageMetadataCommand.ExecuteAsync(null);
        }
        finally
        {
            Cursor = previousCursor;
            if (sender is System.Windows.Controls.Control completedControl)
            {
                completedControl.IsEnabled = true;
            }
        }
    }

    private void ColumnDragGripPreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed ||
            sender is not System.Windows.FrameworkElement { DataContext: LibraryColumnChoiceViewModel { IsSelected: true } choice })
        {
            return;
        }

        System.Windows.DragDrop.DoDragDrop(
            (System.Windows.DependencyObject)sender,
            new System.Windows.DataObject(ColumnChoiceDragFormat, choice),
            System.Windows.DragDropEffects.Move);
    }

    private void ColumnChoiceDragOver(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(ColumnChoiceDragFormat)
            ? System.Windows.DragDropEffects.Move
            : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private async void ColumnChoiceDrop(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(ColumnChoiceDragFormat) ||
            e.Data.GetData(ColumnChoiceDragFormat) is not LibraryColumnChoiceViewModel draggedChoice ||
            sender is not System.Windows.FrameworkElement { DataContext: LibraryColumnChoiceViewModel targetChoice })
        {
            return;
        }

        e.Handled = true;
        await libraryViewModel.ReorderColumnChoiceAsync(draggedChoice, targetChoice);
    }
}

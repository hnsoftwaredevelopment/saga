using EbookManager.App.Services;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class SettingsWindow : System.Windows.Window
{
    private readonly SettingsViewModel viewModel;
    private readonly LibraryViewModel libraryViewModel;
    private readonly LocalizationService localizationService;
    private readonly ThemeService themeService;
    private bool isLoadingSettings;
    private string originalTheme = "Light";
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

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        isLoadingSettings = true;
        await viewModel.LoadAsync();
        originalTheme = viewModel.Theme;
        originalSelectedView = libraryViewModel.SelectedView;
        isLoadingSettings = false;
        localizationService.ApplyCulture(viewModel.Culture);
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
}

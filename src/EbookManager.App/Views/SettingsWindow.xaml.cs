using EbookManager.App.Services;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class SettingsWindow : System.Windows.Window
{
    private readonly SettingsViewModel viewModel;
    private readonly LibraryViewModel libraryViewModel;
    private readonly LocalizationService localizationService;
    private readonly ThemeService themeService;

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
        await viewModel.LoadAsync();
        localizationService.ApplyCulture(viewModel.Culture);
    }

    private void CultureSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        localizationService.ApplyCulture(viewModel.Culture);
    }

    private async void SaveClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.SaveAsync();
        localizationService.ApplyCulture(viewModel.Culture);
        themeService.ApplyTheme(viewModel.Theme);
        DialogResult = true;
    }

    private void CancelClicked(object sender, System.Windows.RoutedEventArgs e)
    {
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

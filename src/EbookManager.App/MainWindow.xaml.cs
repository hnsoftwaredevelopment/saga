using EbookManager.App.Services;
using EbookManager.App.Views;
using EbookManager.Presentation.ViewModels;
using System.Windows.Media.Imaging;

namespace EbookManager.App;

public partial class MainWindow : System.Windows.Window
{
    private readonly LibraryViewModel viewModel;
    private readonly SettingsViewModel settingsViewModel;
    private readonly LocalizationService localizationService;
    private readonly ThemeService themeService;
    private bool isHandlingDrop;
    private bool initialRefreshCompleted;

    public MainWindow(
        LibraryViewModel viewModel,
        SettingsViewModel settingsViewModel,
        LocalizationService localizationService,
        ThemeService themeService)
    {
        this.viewModel = viewModel;
        this.settingsViewModel = settingsViewModel;
        this.localizationService = localizationService;
        this.themeService = themeService;
        InitializeComponent();
        Icon = BitmapFrame.Create(new Uri(
            "pack://application:,,,/Resources/AppIcon/appicon.ico",
            UriKind.Absolute));
        DataContext = viewModel;
        RegisterLibraryDragDropHandlers();
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    public LibraryViewModel ViewModel => viewModel;

    public async Task LoadInitialLibraryAsync()
    {
        initialRefreshCompleted = true;
        await viewModel.RefreshAsync();
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (initialRefreshCompleted)
        {
            return;
        }

        await viewModel.RefreshAsync();
    }

    private void OpenSettingsClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        var window = new SettingsWindow(settingsViewModel, viewModel, localizationService, themeService)
        {
            Owner = this
        };
        window.ShowDialog();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!viewModel.HasActiveImport)
        {
            return;
        }

        var result = System.Windows.MessageBox.Show(
            localizationService.GetString("ImportInProgressCloseMessage"),
            localizationService.GetString("ImportInProgressTitle"),
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            e.Cancel = true;
            return;
        }

        viewModel.CancelImportCommand.Execute(null);
    }

    private void LibraryDropZoneDragOver(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)
            ? System.Windows.DragDropEffects.Copy
            : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private async void LibraryDropZoneDrop(object sender, System.Windows.DragEventArgs e)
    {
        if (isHandlingDrop)
        {
            e.Handled = true;
            return;
        }

        if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] paths)
        {
            e.Handled = true;
            isHandlingDrop = true;
            try
            {
                await viewModel.ImportFilesAsync(paths);
            }
            finally
            {
                isHandlingDrop = false;
            }
        }
    }

    private void RegisterLibraryDragDropHandlers()
    {
        var dragHandler = new System.Windows.DragEventHandler(LibraryDropZoneDragOver);
        AddHandler(System.Windows.DragDrop.PreviewDragEnterEvent, dragHandler, handledEventsToo: true);
        AddHandler(System.Windows.DragDrop.PreviewDragOverEvent, dragHandler, handledEventsToo: true);
        AddHandler(System.Windows.DragDrop.DragEnterEvent, dragHandler, handledEventsToo: true);
        AddHandler(System.Windows.DragDrop.DragOverEvent, dragHandler, handledEventsToo: true);

        var dropHandler = new System.Windows.DragEventHandler(LibraryDropZoneDrop);
        AddHandler(System.Windows.DragDrop.PreviewDropEvent, dropHandler, handledEventsToo: true);
        AddHandler(System.Windows.DragDrop.DropEvent, dropHandler, handledEventsToo: true);
    }
}

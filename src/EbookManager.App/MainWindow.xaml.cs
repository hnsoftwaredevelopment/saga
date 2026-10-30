using EbookManager.App.Services;
using EbookManager.App.Views;
using EbookManager.Presentation.ViewModels;
using EbookManager.Infrastructure.Files;
using EbookManager.Infrastructure.Persistence;
using EbookManager.Infrastructure.Persistence.Repositories;
using EbookManager.Libraries;
using EbookManager.Application.Authors;
using EbookManager.Application.Books;
using EbookManager.Application.Series;
using EbookManager.Application.Tags;

namespace EbookManager.App;

public partial class MainWindow : System.Windows.Window
{
    private readonly LibraryViewModel viewModel;
    private readonly SettingsViewModel settingsViewModel;
    private readonly LocalizationService localizationService;
    private readonly ThemeService themeService;
    private readonly CurrentLibrary currentLibrary;
    private readonly LibraryDbContextFactory contextFactory;
    private readonly BookService bookService;
    private bool isHandlingDrop;
    private bool initialRefreshCompleted;
    private bool isClosingAfterPendingGroupingSave;

    public MainWindow(
        LibraryViewModel viewModel,
        SettingsViewModel settingsViewModel,
        LocalizationService localizationService,
        ThemeService themeService,
        CurrentLibrary currentLibrary,
        LibraryDbContextFactory contextFactory,
        BookService bookService)
    {
        this.viewModel = viewModel;
        this.settingsViewModel = settingsViewModel;
        this.localizationService = localizationService;
        this.themeService = themeService;
        this.currentLibrary = currentLibrary;
        this.contextFactory = contextFactory;
        this.bookService = bookService;
        InitializeComponent();
        DataContext = viewModel;
        RegisterLibraryDragDropHandlers();
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    public LibraryViewModel ViewModel => viewModel;

    private async void OpenTagsClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        var library = currentLibrary.Current;
        if (library is null) return;
        var repository = new EfTagRepository(contextFactory, library.DirectoryPath);
        using var tags = new TagManagementViewModel(repository, new TagRenameService(repository, bookService), localizationService.GetString,
            new TagAssignmentService(repository, bookService));
        new TagManagementWindow(tags) { Owner = this }.ShowDialog();
        try { await viewModel.ReconcileExternalMetadataChangesAsync(tags.ChangedBookIds); }
        catch (Exception)
        {
            System.Windows.MessageBox.Show(this, localizationService.GetString("TagRenameRefreshFailed"),
                localizationService.GetString("TagManagementTitle"));
        }
    }

    private async void OpenSeriesClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        var library = currentLibrary.Current;
        if (library is null) return;
        var repository = new EfSeriesRepository(contextFactory, library.DirectoryPath);
        using var series = new SeriesManagementViewModel(repository, new SeriesRenameService(repository, bookService), localizationService.GetString);
        new SeriesManagementWindow(series) { Owner = this }.ShowDialog();
        try { await viewModel.ReconcileExternalMetadataChangesAsync(series.ChangedBookIds); }
        catch (Exception)
        {
            System.Windows.MessageBox.Show(this, localizationService.GetString("SeriesRenameRefreshFailed"),
                localizationService.GetString("SeriesManagementTitle"));
        }
    }

    private async void OpenAuthorsClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        var library = currentLibrary.Current;
        if (library is null) return;
        // A modal window keeps operations bound to this library while the profile is being edited.
        var repository = new EfAuthorProfileRepository(contextFactory, library.DirectoryPath);
        var photos = new ManagedAuthorPhotoStore(library.DirectoryPath);
        using var authors = new AuthorManagementViewModel(repository,
            photos, localizationService.GetString,
            new AuthorRenameService(repository, bookService), new AuthorMergeService(repository, repository, photos, bookService));
        new AuthorManagementWindow(authors) { Owner = this }.ShowDialog();
        try { await viewModel.ReconcileExternalMetadataChangesAsync(authors.ChangedBookIds); }
        catch (Exception)
        {
            System.Windows.MessageBox.Show(this, localizationService.GetString("AuthorRenameRefreshFailed"),
                localizationService.GetString("AuthorManagementTitle"));
        }
    }

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

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!isClosingAfterPendingGroupingSave && viewModel.HasPendingGroupingSettingsSave)
        {
            e.Cancel = true;
            await viewModel.WaitForPendingGroupingSettingsSaveAsync();
            isClosingAfterPendingGroupingSave = true;
            Close();
            return;
        }

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

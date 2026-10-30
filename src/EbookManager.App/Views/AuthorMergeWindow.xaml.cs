using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using EbookManager.App.Services;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class AuthorMergeWindow : Window
{
    private readonly AuthorMergeDialogViewModel viewModel;
    private readonly Func<Guid, string, Task<byte[]?>> readPhoto;
    private int photoVersion;
    private bool closed;
    private readonly SemaphoreSlim comparisonLoads = new(2);
    public AuthorMergeWindow(AuthorMergeDialogViewModel viewModel, Func<Guid, string, Task<byte[]?>> readPhoto)
    {
        this.viewModel = viewModel;
        this.readPhoto = readPhoto;
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += ChoiceChanged;
        Loaded += async (_, _) => { TargetBox.Focus(); await UpdatePhotoAsync(); };
        Closed += (_, _) => { closed = true; ++photoVersion; viewModel.PropertyChanged -= ChoiceChanged; };
    }
    private async void ChoiceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(viewModel.PhotoChoice)) await UpdatePhotoAsync();
    }
    private async Task UpdatePhotoAsync()
    {
        var version = ++photoVersion;
        PhotoPreview.Source = null;
        PhotoError.Visibility = Visibility.Collapsed;
        viewModel.PhotoUnavailable = false;
        viewModel.PhotoLoading = false;
        var choice = viewModel.PhotoChoice;
        if (choice?.AuthorId is not Guid id || choice.Value is not string path) return;
        viewModel.PhotoLoading = true;
        try
        {
            var bytes = await readPhoto(id, path);
            if (version != photoVersion || !IsLoaded) return;
            PhotoPreview.Source = AuthorPhotoImageProcessor.Preview(bytes);
            if (PhotoPreview.Source is null) throw new InvalidDataException("Photo unavailable.");
        }
        catch (Exception)
        {
            if (version != photoVersion || !IsLoaded) return;
            viewModel.PhotoUnavailable = true;
            PhotoError.Visibility = Visibility.Visible;
        }
        finally { if (version == photoVersion && !closed) viewModel.PhotoLoading = false; }
    }
    private void ConfirmClicked(object sender, RoutedEventArgs e) { if (viewModel.CanMerge) DialogResult = true; }
    private async void ComparisonPhotoLoaded(object sender, RoutedEventArgs e) => await LoadComparisonPhotoAsync((Image)sender);
    private async void ComparisonPhotoChanged(object sender, DependencyPropertyChangedEventArgs e)
    { if (((Image)sender).IsLoaded) await LoadComparisonPhotoAsync((Image)sender); }
    private void ComparisonPhotoUnloaded(object sender, RoutedEventArgs e) { var image = (Image)sender; image.Tag = null; image.Source = null; }
    private async Task LoadComparisonPhotoAsync(Image image)
    {
        var version = new object();
        image.Tag = version;
        image.Source = null;
        image.ToolTip = null;
        if (image.DataContext is not AuthorComparisonCard card || card.Profile.PhotoRelativePath is not string path) return;
        await comparisonLoads.WaitAsync();
        try
        {
            if (closed || !image.IsLoaded || !ReferenceEquals(image.Tag, version)) return;
            var bytes = await readPhoto(card.Profile.Id, path);
            if (closed || !image.IsLoaded || !ReferenceEquals(image.Tag, version)) return;
            image.Source = AuthorPhotoImageProcessor.Preview(bytes);
            if (image.Source is null) image.ToolTip = Localization.LocalizedStrings.Current["AuthorPhotoLoadFailed"];
        }
        catch (Exception) { if (!closed && ReferenceEquals(image.Tag, version)) image.ToolTip = Localization.LocalizedStrings.Current["AuthorPhotoLoadFailed"]; }
        finally { comparisonLoads.Release(); }
    }
}

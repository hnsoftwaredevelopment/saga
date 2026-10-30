using System.ComponentModel;
using System.IO;
using System.Windows;
using EbookManager.App.Services;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class AuthorMergeWindow : Window
{
    private readonly AuthorMergeDialogViewModel viewModel;
    private readonly Func<Guid, string, Task<byte[]?>> readPhoto;
    private int photoVersion;
    public AuthorMergeWindow(AuthorMergeDialogViewModel viewModel, Func<Guid, string, Task<byte[]?>> readPhoto)
    {
        this.viewModel = viewModel;
        this.readPhoto = readPhoto;
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += ChoiceChanged;
        Loaded += async (_, _) => { TargetBox.Focus(); await UpdatePhotoAsync(); };
        Closed += (_, _) => { ++photoVersion; viewModel.PropertyChanged -= ChoiceChanged; };
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
        var choice = viewModel.PhotoChoice;
        if (choice?.AuthorId is not Guid id || choice.Value is not string path) return;
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
    }
    private void ConfirmClicked(object sender, RoutedEventArgs e) { if (viewModel.CanMerge) DialogResult = true; }
}

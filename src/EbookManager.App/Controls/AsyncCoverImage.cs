using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EbookManager.Presentation.Images;

namespace EbookManager.App.Controls;

public sealed class AsyncCoverImage : Image
{
    private const int ThumbnailCacheCapacity = 256;
    private static readonly CoverThumbnailCache<ImageSource> ThumbnailCache =
        new(ThumbnailCacheCapacity, LoadThumbnailAsync);
    private CancellationTokenSource? loadCancellation;

    public static readonly DependencyProperty SourcePathProperty =
        DependencyProperty.Register(
            nameof(SourcePath),
            typeof(string),
            typeof(AsyncCoverImage),
            new PropertyMetadata(null, OnLoadPropertyChanged));

    public static readonly DependencyProperty DecodePixelWidthProperty =
        DependencyProperty.Register(
            nameof(DecodePixelWidth),
            typeof(int),
            typeof(AsyncCoverImage),
            new PropertyMetadata(160, OnLoadPropertyChanged, CoerceDecodePixelWidth));

    public AsyncCoverImage()
    {
        Loaded += (_, _) => BeginLoad();
        Unloaded += (_, _) => CancelPendingLoad();
    }

    public string? SourcePath
    {
        get => (string?)GetValue(SourcePathProperty);
        set => SetValue(SourcePathProperty, value);
    }

    public int DecodePixelWidth
    {
        get => (int)GetValue(DecodePixelWidthProperty);
        set => SetValue(DecodePixelWidthProperty, value);
    }

    private static void OnLoadPropertyChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e) =>
        ((AsyncCoverImage)dependencyObject).BeginLoad();

    private static object CoerceDecodePixelWidth(DependencyObject dependencyObject, object baseValue) =>
        baseValue is int width && width > 0 ? width : 160;

    private void BeginLoad()
    {
        CancelPendingLoad();
        Source = null;
        if (!IsLoaded || string.IsNullOrWhiteSpace(SourcePath))
        {
            return;
        }

        var path = SourcePath;
        var width = DecodePixelWidth;
        var cancellation = new CancellationTokenSource();
        loadCancellation = cancellation;
        _ = LoadSourceAsync(path, width, cancellation);
    }

    private async Task LoadSourceAsync(
        string path,
        int width,
        CancellationTokenSource cancellation)
    {
        try
        {
            var source = await ThumbnailCache.GetAsync(path, width, cancellation.Token);
            if (!cancellation.IsCancellationRequested &&
                ReferenceEquals(loadCancellation, cancellation) &&
                string.Equals(SourcePath, path, StringComparison.OrdinalIgnoreCase) &&
                DecodePixelWidth == width)
            {
                Source = source;
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        finally
        {
            if (ReferenceEquals(loadCancellation, cancellation))
            {
                loadCancellation = null;
            }

            cancellation.Dispose();
        }
    }

    private void CancelPendingLoad()
    {
        var cancellation = loadCancellation;
        loadCancellation = null;
        cancellation?.Cancel();
    }

    private static Task<ImageSource?> LoadThumbnailAsync(
        string path,
        int decodePixelWidth,
        CancellationToken cancellationToken) =>
        Task.Run<ImageSource?>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                using var stream = File.OpenRead(path);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.DecodePixelWidth = decodePixelWidth;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                cancellationToken.ThrowIfCancellationRequested();
                return image;
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or NotSupportedException or
                    InvalidOperationException or FormatException)
            {
                return null;
            }
        }, cancellationToken);
}

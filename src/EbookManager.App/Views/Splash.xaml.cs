using System.Windows;
using System.Windows.Media.Imaging;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Views;

public partial class Splash : Window
{
    public Splash(
        string subtitle,
        string version,
        string status)
    {
        InitializeComponent();
        AppLogoImage.Source = LoadAppIcon();
        SubtitleText.Text = subtitle;
        VersionText.Text = version;
        StatusText.Text = status;
    }

    public void BindLibraryProgress(LibraryViewModel viewModel, string status)
    {
        DataContext = viewModel;
        StatusText.Text = status;
    }

    private static BitmapSource LoadAppIcon()
    {
        var decoder = new IconBitmapDecoder(
            new Uri("pack://application:,,,/EbookManager;component/Resources/AppIcon/appicon.ico"),
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);

        return decoder.Frames
            .OrderByDescending(frame => frame.PixelWidth)
            .ThenByDescending(frame => frame.PixelHeight)
            .First();
    }
}

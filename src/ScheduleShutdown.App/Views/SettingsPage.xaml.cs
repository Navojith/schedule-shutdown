using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using ScheduleShutdown.App.Services;
using ScheduleShutdown.App.ViewModels;

namespace ScheduleShutdown.App.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
        AppIconImage.Source = new BitmapImage(new Uri(AppAssets.IconPngPath));
    }

    public SettingsViewModel ViewModel { get; }
}

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using ScheduleShutdown.App.Services;

namespace ScheduleShutdown.App.Views;

public sealed partial class MainWindow : Window
{
    public const int WidthDips = 460;
    public const int HeightDips = 620;

    public MainWindow(AppEnvironment environment)
    {
        InitializeComponent();

        if (environment.IsDryRun)
        {
            Title += " (dry run)";
            AppTitleBar.Subtitle = "Dry run";
        }

        TitleIcon.ImageSource = new BitmapImage(new Uri(AppAssets.IconPngPath));
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        WindowSizing.Resize(this, WidthDips, HeightDips);
        WindowSizing.SetMinimumSize(this, WidthDips, HeightDips);
        WindowSizing.KeepInWorkArea(this);
        WindowSizing.PreventMaximize(this);

        ContentFrame.Navigate(typeof(MainPage));
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e) =>
        ContentFrame.Navigate(typeof(SettingsPage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });

    private void AppTitleBar_BackRequested(TitleBar sender, object args)
    {
        if (ContentFrame.CanGoBack)
            ContentFrame.GoBack();
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        AppTitleBar.IsBackButtonVisible = ContentFrame.CanGoBack;
        SettingsButton.Visibility = e.SourcePageType == typeof(SettingsPage) ? Visibility.Collapsed : Visibility.Visible;
    }
}

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using ScheduleShutdown.App.Services;
using Windows.Graphics;

namespace ScheduleShutdown.App.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow(AppEnvironment environment)
    {
        InitializeComponent();

        if (environment.IsDryRun)
        {
            Title += " (dry run)";
            TitleText.Text = Title;
        }

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        AppWindow.Resize(new SizeInt32(460, 620));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
        }
    }
}

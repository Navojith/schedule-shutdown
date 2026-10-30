using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using ScheduleShutdown.App.Services;

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

        WindowSizing.Resize(this, 460, 620);
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
        }
    }
}

using Microsoft.UI.Xaml;
using ScheduleShutdown.Core.Execution;

namespace ScheduleShutdown.App;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        if (DryRunMode.IsEnabledForCurrentProcess())
            _window.Title += " (dry run)";
        _window.Activate();
    }
}

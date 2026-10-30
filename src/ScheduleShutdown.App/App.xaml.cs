using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using ScheduleShutdown.App.Services;
using ScheduleShutdown.App.ViewModels;
using ScheduleShutdown.App.Views;
using ScheduleShutdown.Core.Abstractions;
using ScheduleShutdown.Core.Execution;
using ScheduleShutdown.Core.Scheduling;

namespace ScheduleShutdown.App;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Services = ConfigureServices(DispatcherQueue.GetForCurrentThread());

        var environment = Services.GetRequiredService<AppEnvironment>();
        Services.GetRequiredService<AppLog>().Write(environment.IsDryRun ? "Started (dry run)" : "Started");

        _window = new MainWindow(environment);
        _window.Activate();
    }

    private static ServiceProvider ConfigureServices(DispatcherQueue dispatcherQueue)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new AppEnvironment(DryRunMode.IsEnabledForCurrentProcess()));
        services.AddSingleton<AppLog>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ITimerFactory>(new DispatcherQueueTimerFactory(dispatcherQueue));
        services.AddSingleton(sp => ShutdownExecutorFactory.Create(sp.GetRequiredService<AppLog>()));
        services.AddSingleton<IStateStore, InMemoryStateStore>();
        services.AddSingleton<ShutdownScheduler>();
        services.AddSingleton<MainViewModel>();
        return services.BuildServiceProvider();
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using ScheduleShutdown.App.Services;
using ScheduleShutdown.App.ViewModels;
using ScheduleShutdown.Core.Abstractions;
using ScheduleShutdown.Core.Execution;
using ScheduleShutdown.Core.Persistence;
using ScheduleShutdown.Core.Scheduling;

namespace ScheduleShutdown.App;

public partial class App : Application
{
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

        var settings = Services.GetRequiredService<JsonSettingsStore>().Load();
        Services.GetRequiredService<ThemeService>().SetTheme(settings.Theme);

        Services.GetRequiredService<AppShell>().Start();
    }

    private static ServiceProvider ConfigureServices(DispatcherQueue dispatcherQueue)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new AppEnvironment(DryRunMode.IsEnabledForCurrentProcess()));
        services.AddSingleton<AppLog>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ITimerFactory>(new DispatcherQueueTimerFactory(dispatcherQueue));
        services.AddSingleton(sp => ShutdownExecutorFactory.Create(sp.GetRequiredService<AppLog>()));
        services.AddSingleton<IStateStore>(sp => CreateStateStore(sp.GetRequiredService<AppEnvironment>(), sp.GetRequiredService<AppLog>()));
        services.AddSingleton<ShutdownScheduler>();
        services.AddSingleton(sp => new JsonSettingsStore(DataPath("settings.json"), sp.GetRequiredService<AppLog>().Write));
        services.AddSingleton<ThemeService>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton(sp => new NotificationService(dispatcherQueue, sp.GetRequiredService<AppLog>()));
        services.AddSingleton<TrayIconService>();
        services.AddSingleton<AppShell>();
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Dry run keeps its own file, so a schedule saved during development is never restored by a normal
    /// launch and turned into a real shutdown.
    /// </summary>
    private static JsonStateStore CreateStateStore(AppEnvironment environment, AppLog log) =>
        new(DataPath(environment.IsDryRun ? "state.dryrun.json" : "state.json"), log.Write);

    private static string DataPath(string fileName) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ScheduleShutdown", fileName);
}

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ScheduleShutdown.App.ViewModels;
using ScheduleShutdown.App.Views;
using ScheduleShutdown.Core.Formatting;
using ScheduleShutdown.Core.Scheduling;

namespace ScheduleShutdown.App.Services;

/// <summary>
/// Owns the windows, tray icon and toasts: close-to-tray, the final countdown window, warning toasts
/// and the Exit confirmation. All calls happen on the UI thread.
/// </summary>
public sealed class AppShell(
    ShutdownScheduler scheduler,
    MainViewModel mainViewModel,
    NotificationService notifications,
    TrayIconService tray,
    AppEnvironment environment,
    AppLog log)
{
    private MainWindow? _mainWindow;
    private FinalCountdownWindow? _countdownWindow;
    private bool _exiting;
    private bool _confirmingExit;
    private bool _trayHintShown;

    public void Start()
    {
        notifications.ActionInvoked += OnNotificationAction;
        notifications.Register();

        _mainWindow = new MainWindow(environment);
        _mainWindow.AppWindow.Closing += OnMainWindowClosing;

        tray.Create(ToolTip(), new RelayCommand(ShowMainWindow), mainViewModel.CancelCommand, new RelayCommand(RequestExit));

        scheduler.StateChanged += OnStateChanged;
        scheduler.Tick += (_, _) => tray.SetToolTip(ToolTip());
        scheduler.WarningReached += (_, lead) => notifications.ShowWarning(lead, scheduler.Current?.Options.Force == true);
        scheduler.FinalCountdownStarted += (_, _) => ShowCountdownWindow();
        scheduler.DueReached += (_, _) => OnDueReached();
        scheduler.ShutdownFailed += (_, _) => ShowMainWindow();

        _mainWindow.Activate();
    }

    public void ShowMainWindow()
    {
        if (_mainWindow is null || _exiting)
            return;

        WindowFocus.BringToFront(_mainWindow);
    }

    private void OnMainWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_exiting)
            return;

        // Closing hides to the tray so the schedule keeps running.
        args.Cancel = true;
        sender.Hide();

        if (!_trayHintShown)
        {
            _trayHintShown = true;
            notifications.ShowInfo("Schedule Shutdown is still running", "It's in the notification area. Right-click its icon to exit.");
        }
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (!scheduler.IsActive)
            notifications.RemoveWarnings();
        tray.SetToolTip(ToolTip());
    }

    private void ShowCountdownWindow()
    {
        if (_countdownWindow is not null || _exiting)
            return;

        _countdownWindow = new FinalCountdownWindow(new CountdownViewModel(scheduler, mainViewModel));
        _countdownWindow.Closed += (_, _) => _countdownWindow = null;
        _countdownWindow.Activate();
    }

    private void OnDueReached()
    {
        if (environment.IsDryRun)
            notifications.ShowInfo("Dry run: no shutdown", "The shutdown command was written to app.log instead of being run.");
    }

    private void OnNotificationAction(object? sender, NotificationAction action)
    {
        switch (action)
        {
            case NotificationAction.Postpone when mainViewModel.PostponeCommand.CanExecute(null):
                mainViewModel.PostponeCommand.Execute(null);
                break;
            case NotificationAction.Cancel:
                mainViewModel.CancelCommand.Execute(null);
                break;
            default:
                ShowMainWindow();
                break;
        }
    }

    private async void RequestExit()
    {
        if (_mainWindow is null || _exiting || _confirmingExit)
            return;

        if (scheduler.IsActive)
        {
            ShowMainWindow();
            _confirmingExit = true;
            ContentDialogResult result;
            try
            {
                var dialog = new ContentDialog
                {
                    XamlRoot = _mainWindow.Content.XamlRoot,
                    Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
                    Title = "Exit and cancel the shutdown?",
                    Content = "The scheduled shutdown only happens while Schedule Shutdown is running. Exiting cancels it.",
                    PrimaryButtonText = "Exit and cancel",
                    CloseButtonText = "Keep running",
                    DefaultButton = ContentDialogButton.Close,
                };
                result = await dialog.ShowAsync();
            }
            finally
            {
                _confirmingExit = false;
            }

            if (result != ContentDialogResult.Primary)
                return;

            mainViewModel.CancelCommand.Execute(null);
        }

        Exit();
    }

    private void Exit()
    {
        _exiting = true;
        log.Write("Exiting");
        _countdownWindow?.Close();
        tray.Dispose();
        notifications.Dispose();
        scheduler.Dispose();
        _mainWindow?.Close();
        Application.Current.Exit();
    }

    private string ToolTip() => scheduler.IsActive
        ? $"Schedule Shutdown\nShuts down in {ScheduleFormatter.FormatRemaining(scheduler.Remaining)}"
        : "Schedule Shutdown\nNo shutdown scheduled";
}

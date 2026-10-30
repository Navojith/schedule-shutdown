using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using ScheduleShutdown.Core.Formatting;

namespace ScheduleShutdown.App.Services;

/// <summary>What the user picked on a toast.</summary>
public enum NotificationAction
{
    Open,
    Postpone,
    Cancel,
}

/// <summary>Windows toasts through <see cref="AppNotificationManager"/>. Clicks are raised on the UI thread.</summary>
public sealed class NotificationService(DispatcherQueue dispatcherQueue, AppLog log) : IDisposable
{
    private const string ActionKey = "action";

    /// <summary>Shared by warning toasts, so the 1-minute warning replaces the 10-minute one.</summary>
    private const string WarningTag = "warning";

    private bool _registered;

    public event EventHandler<NotificationAction>? ActionInvoked;

    public void Register()
    {
        try
        {
            var manager = AppNotificationManager.Default;
            manager.NotificationInvoked += OnNotificationInvoked;
            // Unpackaged apps have no manifest, so the toast header's name and icon come from here.
            manager.Register(AppAssets.DisplayName, new Uri(AppAssets.IconPngPath));
            _registered = true;
        }
        catch (Exception ex)
        {
            // Toasts are a convenience: the countdown dialog still warns without them.
            log.Write($"Notifications unavailable: {ex.Message}");
        }
    }

    public void ShowWarning(TimeSpan lead, bool force)
    {
        var builder = new AppNotificationBuilder()
            .AddArgument(ActionKey, nameof(NotificationAction.Open))
            .AddText($"Your PC will shut down in {ScheduleFormatter.DescribeLeadTime(lead)}")
            .AddText(force ? "Force shutdown is on: unsaved work will be lost." : "Save your work.")
            .AddButton(new AppNotificationButton("Postpone 10 min").AddArgument(ActionKey, nameof(NotificationAction.Postpone)))
            .AddButton(new AppNotificationButton("Cancel shutdown").AddArgument(ActionKey, nameof(NotificationAction.Cancel)));
        Show(builder, WarningTag);
    }

    public void ShowInfo(string title, string message) =>
        Show(new AppNotificationBuilder()
            .AddArgument(ActionKey, nameof(NotificationAction.Open))
            .AddText(title)
            .AddText(message), tag: null);

    /// <summary>Removes warning toasts so stale Postpone/Cancel buttons can't act on a later schedule.</summary>
    public async void RemoveWarnings()
    {
        if (!_registered)
            return;

        try
        {
            await AppNotificationManager.Default.RemoveByTagAsync(WarningTag);
        }
        catch (Exception ex)
        {
            log.Write($"Couldn't remove notifications: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (!_registered)
            return;

        _registered = false;
        AppNotificationManager.Default.NotificationInvoked -= OnNotificationInvoked;
        AppNotificationManager.Default.Unregister();
    }

    private void Show(AppNotificationBuilder builder, string? tag)
    {
        if (!_registered)
            return;

        try
        {
            var notification = builder.BuildNotification();
            if (tag is not null)
                notification.Tag = tag;
            AppNotificationManager.Default.Show(notification);
        }
        catch (Exception ex)
        {
            log.Write($"Couldn't show notification: {ex.Message}");
        }
    }

    /// <summary>What a toast click asks for. Also used for clicks that launched or reached the app as an activation.</summary>
    public static NotificationAction ParseAction(AppNotificationActivatedEventArgs args) =>
        args.Arguments.TryGetValue(ActionKey, out var value) && Enum.TryParse<NotificationAction>(value, out var parsed)
            ? parsed
            : NotificationAction.Open;

    /// <summary>Runs on a background thread.</summary>
    private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        var action = ParseAction(args);
        dispatcherQueue.TryEnqueue(() => ActionInvoked?.Invoke(this, action));
    }
}

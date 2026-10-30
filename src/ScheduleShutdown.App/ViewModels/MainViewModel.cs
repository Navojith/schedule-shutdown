using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScheduleShutdown.App.Services;
using ScheduleShutdown.Core.Abstractions;
using ScheduleShutdown.Core.Formatting;
using ScheduleShutdown.Core.Models;
using ScheduleShutdown.Core.Scheduling;

namespace ScheduleShutdown.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    public static readonly TimeSpan PostponeAmount = TimeSpan.FromMinutes(10);

    private readonly ShutdownScheduler _scheduler;
    private readonly IClock _clock;
    private readonly AppEnvironment _environment;
    private readonly AppLog _log;

    /// <summary>Remaining time when the progress ring was full; grows when postponed.</summary>
    private TimeSpan _countdownTotal;

    public MainViewModel(ShutdownScheduler scheduler, IClock clock, AppEnvironment environment, AppLog log)
    {
        _scheduler = scheduler;
        _clock = clock;
        _environment = environment;
        _log = log;

        var now = TimeZoneInfo.ConvertTime(clock.Now, clock.LocalTimeZone);
        SelectedTime = DefaultTime(now.TimeOfDay);
        TimerHours = 0;
        TimerMinutes = 30;
        RemainingText = SummaryText = ErrorMessage = InfoMessage = "";

        _scheduler.StateChanged += (_, _) => RefreshSchedule();
        _scheduler.Tick += (_, remaining) => UpdateCountdown(remaining);
        _scheduler.DueReached += (_, _) => OnDueReached();
        _scheduler.ShutdownFailed += (_, ex) => ShowError($"Couldn't shut down: {ex.Message}");
        RefreshSchedule();
    }

    // ---- Form ----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAtTimeMode), nameof(IsTimerMode))]
    public partial ScheduleMode Mode { get; set; }

    public bool IsAtTimeMode => Mode == ScheduleMode.AtTime;

    public bool IsTimerMode => Mode == ScheduleMode.Timer;

    [ObservableProperty]
    public partial TimeSpan SelectedTime { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? SelectedDate { get; set; }

    [ObservableProperty]
    public partial double TimerHours { get; set; }

    [ObservableProperty]
    public partial double TimerMinutes { get; set; }

    [ObservableProperty]
    public partial bool Force { get; set; }

    // ---- Active schedule ----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFormVisible))]
    [NotifyCanExecuteChangedFor(nameof(PostponeCommand), nameof(CancelCommand))]
    public partial bool IsScheduled { get; set; }

    public bool IsFormVisible => !IsScheduled;

    [ObservableProperty]
    public partial string RemainingText { get; set; }

    [ObservableProperty]
    public partial string SummaryText { get; set; }

    /// <summary>Progress ring value, 0–100: the share of the countdown still left.</summary>
    [ObservableProperty]
    public partial double ProgressValue { get; set; }

    // ---- Messages ----

    [ObservableProperty]
    public partial bool IsErrorOpen { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool IsInfoOpen { get; set; }

    [ObservableProperty]
    public partial string InfoMessage { get; set; }

    [RelayCommand]
    private void Schedule()
    {
        IsErrorOpen = false;
        IsInfoOpen = false;

        var options = new ShutdownOptions(Force);
        ScheduleRequest request;
        if (Mode == ScheduleMode.AtTime)
        {
            if (SelectedDate is not { } date)
            {
                ShowError("Pick a date.");
                return;
            }

            request = ScheduleRequest.AtTime(DateOnly.FromDateTime(date.Date), TimeOnly.FromTimeSpan(SelectedTime), options);
        }
        else
        {
            request = ScheduleRequest.Timer(TimeSpan.FromHours(Whole(TimerHours)) + TimeSpan.FromMinutes(Whole(TimerMinutes)), options);
        }

        var result = _scheduler.Schedule(request);
        if (result.Error is { } error)
        {
            ShowError(Describe(error));
            return;
        }

        _log.Write($"Scheduled shutdown for {result.Target:yyyy-MM-dd HH:mm:ss zzz} (force: {options.Force})");
    }

    [RelayCommand]
    private void ApplyPreset(string minutes)
    {
        var total = int.Parse(minutes, CultureInfo.InvariantCulture);
        TimerHours = total / 60;
        TimerMinutes = total % 60;
    }

    [RelayCommand(CanExecute = nameof(IsScheduled))]
    private void Postpone()
    {
        _scheduler.Postpone(PostponeAmount);
        _log.Write($"Postponed shutdown to {_scheduler.Current?.Target:yyyy-MM-dd HH:mm:ss zzz}");
    }

    [RelayCommand(CanExecute = nameof(IsScheduled))]
    private void Cancel()
    {
        if (!_scheduler.Cancel())
            return;

        // A restore message is about the schedule that just ended.
        IsInfoOpen = false;
        _log.Write("Cancelled shutdown");
    }

    public void ShowInfo(string message)
    {
        InfoMessage = message;
        IsInfoOpen = true;
    }

    partial void OnSelectedTimeChanged(TimeSpan value)
    {
        // Picking a time that already passed today moves the date to tomorrow.
        var now = _clock.Now;
        var time = TimeOnly.FromTimeSpan(value);
        if (SelectedDate is { } date &&
            ScheduleCalculator.ToTarget(DateOnly.FromDateTime(date.Date), time, _clock.LocalTimeZone) > now)
        {
            return;
        }

        var suggested = ScheduleCalculator.SuggestDate(time, now, _clock.LocalTimeZone);
        SelectedDate = new DateTimeOffset(suggested.ToDateTime(TimeOnly.MinValue));
    }

    private void RefreshSchedule()
    {
        var wasScheduled = IsScheduled;
        IsScheduled = _scheduler.IsActive;
        if (_scheduler.Current is not { } state)
            return;

        var remaining = _scheduler.Remaining;
        _countdownTotal = wasScheduled && _countdownTotal > remaining ? _countdownTotal : remaining;

        var when = ScheduleFormatter.DescribeTarget(state.Target, _clock.Now, _clock.LocalTimeZone, CultureInfo.CurrentCulture);
        SummaryText = $"Shuts down {when} · Force: {(state.Options.Force ? "On" : "Off")}";
        UpdateCountdown(remaining);
    }

    private void UpdateCountdown(TimeSpan remaining)
    {
        RemainingText = ScheduleFormatter.FormatRemaining(remaining);
        ProgressValue = _countdownTotal > TimeSpan.Zero ? 100 * remaining / _countdownTotal : 0;
    }

    private void OnDueReached()
    {
        if (!_environment.IsDryRun)
            return;

        ShowInfo("Dry run: the shutdown command was written to app.log instead of being run.");
    }

    private void ShowError(string message)
    {
        _log.Write($"Error: {message}");
        ErrorMessage = message;
        IsErrorOpen = true;
    }

    private static string Describe(ScheduleError error) => error switch
    {
        ScheduleError.TimeInPast => "That time has already passed. Pick a later time or date.",
        ScheduleError.TimeTooFarAhead => "Pick a time within the next 7 days.",
        ScheduleError.DurationTooShort => "Set a timer of at least 1 minute.",
        ScheduleError.DurationTooLong => "Set a timer of 7 days or less.",
        _ => "That schedule isn't valid.",
    };

    /// <summary>NumberBox gives NaN when cleared.</summary>
    private static double Whole(double value) => double.IsNaN(value) ? 0 : Math.Floor(Math.Max(value, 0));

    /// <summary>One hour from now, rounded up to the next 5 minutes.</summary>
    private static TimeSpan DefaultTime(TimeSpan timeOfDay)
    {
        var minutes = Math.Ceiling((timeOfDay.TotalMinutes + 60) / 5) * 5;
        return TimeSpan.FromMinutes(minutes % (24 * 60));
    }
}

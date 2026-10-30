using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using ScheduleShutdown.Core.Formatting;
using ScheduleShutdown.Core.Scheduling;

namespace ScheduleShutdown.App.ViewModels;

/// <summary>Drives the final countdown window. One instance per window; dispose it when the window closes.</summary>
public sealed partial class CountdownViewModel : ObservableObject, IDisposable
{
    private readonly ShutdownScheduler _scheduler;

    public CountdownViewModel(ShutdownScheduler scheduler, MainViewModel main)
    {
        _scheduler = scheduler;
        PostponeCommand = main.PostponeCommand;
        CancelCommand = main.CancelCommand;

        var force = scheduler.Current?.Options.Force == true;
        Message = force
            ? "Force shutdown is on: apps will close and unsaved work will be lost."
            : "Save your work. Apps with unsaved changes may stop the shutdown.";
        SecondsText = "";

        _scheduler.Tick += OnTick;
        _scheduler.StateChanged += OnStateChanged;
        UpdateSeconds(_scheduler.Remaining);
    }

    /// <summary>The final countdown ended: cancelled, postponed or shut down.</summary>
    public event EventHandler? Finished;

    public string Message { get; }

    public ICommand PostponeCommand { get; }

    public ICommand CancelCommand { get; }

    [ObservableProperty]
    public partial string SecondsText { get; set; }

    public void Dispose()
    {
        _scheduler.Tick -= OnTick;
        _scheduler.StateChanged -= OnStateChanged;
    }

    private void OnTick(object? sender, TimeSpan remaining) => UpdateSeconds(remaining);

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (!_scheduler.IsInFinalCountdown)
            Finished?.Invoke(this, EventArgs.Empty);
        else
            UpdateSeconds(_scheduler.Remaining);
    }

    private void UpdateSeconds(TimeSpan remaining) =>
        SecondsText = ScheduleFormatter.CountdownSeconds(remaining).ToString(System.Globalization.CultureInfo.CurrentCulture);
}

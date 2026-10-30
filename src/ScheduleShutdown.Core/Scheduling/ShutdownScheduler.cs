using ScheduleShutdown.Core.Abstractions;
using ScheduleShutdown.Core.Models;

namespace ScheduleShutdown.Core.Scheduling;

/// <summary>
/// Owns the active schedule: ticks once per second against an absolute target, raises warnings,
/// runs the final countdown and executes the shutdown. Not thread-safe; use it from the UI thread.
/// </summary>
public sealed class ShutdownScheduler : IDisposable
{
    public static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    /// <summary>Length of the final countdown dialog. The user always gets (about) this long to cancel.</summary>
    public static readonly TimeSpan FinalCountdownDuration = TimeSpan.FromSeconds(60);

    /// <summary>
    /// If the final countdown would start with less than <see cref="FinalCountdownDuration"/> minus this
    /// (sleep/resume, overdue restore, very short schedule), the target moves to now + the full countdown.
    /// </summary>
    public static readonly TimeSpan LateTolerance = TimeSpan.FromSeconds(5);

    /// <summary>Saved targets older than this are discarded on restore instead of shutting down.</summary>
    public static readonly TimeSpan StaleThreshold = TimeSpan.FromHours(12);

    /// <summary>Warning thresholds, largest first.</summary>
    public static readonly IReadOnlyList<TimeSpan> WarningThresholds = [TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1)];

    private readonly IClock _clock;
    private readonly ITimerFactory _timerFactory;
    private readonly IShutdownExecutor _executor;
    private readonly IStateStore _stateStore;
    private readonly HashSet<TimeSpan> _passedWarnings = [];

    private ITickTimer? _timer;
    private int _generation;

    public ShutdownScheduler(IClock clock, ITimerFactory timerFactory, IShutdownExecutor executor, IStateStore stateStore)
    {
        _clock = clock;
        _timerFactory = timerFactory;
        _executor = executor;
        _stateStore = stateStore;
    }

    /// <summary>Active schedule, or null when nothing is scheduled.</summary>
    public ScheduleState? Current { get; private set; }

    public bool IsActive => Current is not null;

    public bool IsInFinalCountdown { get; private set; }

    /// <summary>Time left until shutdown, never negative. Zero when inactive.</summary>
    public TimeSpan Remaining => Current is null ? TimeSpan.Zero : Max(Current.Target - _clock.Now, TimeSpan.Zero);

    /// <summary>Schedule started, cancelled, postponed, moved, or finished.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Raised once per second while active, with the time remaining.</summary>
    public event EventHandler<TimeSpan>? Tick;

    /// <summary>Raised when a <see cref="WarningThresholds"/> entry is reached, with that threshold.</summary>
    public event EventHandler<TimeSpan>? WarningReached;

    /// <summary>Raised when the final countdown starts, with the shutdown moment.</summary>
    public event EventHandler<DateTimeOffset>? FinalCountdownStarted;

    /// <summary>Raised just before the shutdown command runs. The schedule is already cleared.</summary>
    public event EventHandler<ShutdownOptions>? DueReached;

    /// <summary>The shutdown command could not be started.</summary>
    public event EventHandler<Exception>? ShutdownFailed;

    /// <summary>Starts a schedule, replacing any active one.</summary>
    public ScheduleResult Schedule(ScheduleRequest request)
    {
        var result = ScheduleCalculator.Calculate(request, _clock.Now, _clock.LocalTimeZone);
        if (result.Target is { } target)
            Activate(new ScheduleState(target, request.Mode, request.Options));
        return result;
    }

    /// <summary>Restores the persisted schedule. Call once on launch.</summary>
    public RestoreOutcome Restore()
    {
        if (IsActive)
            throw new InvalidOperationException("A schedule is already active.");

        var saved = _stateStore.Load();
        if (saved is null)
            return RestoreOutcome.NothingSaved;

        var now = _clock.Now;
        if (now - saved.Target > StaleThreshold)
        {
            _stateStore.Clear();
            return RestoreOutcome.DiscardedStale;
        }

        var overdue = saved.Target <= now;
        Activate(saved);
        return overdue ? RestoreOutcome.RestoredOverdue : RestoreOutcome.Restored;
    }

    /// <summary>Cancels the active schedule. Returns false if nothing was scheduled.</summary>
    public bool Cancel()
    {
        if (!IsActive)
            return false;

        Deactivate();
        StateChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Moves the target later and leaves the final countdown if it was running.</summary>
    public void Postpone(TimeSpan amount)
    {
        if (Current is null)
            throw new InvalidOperationException("No schedule is active.");
        if (amount <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Must be positive.");

        var now = _clock.Now;
        var target = Max(Current.Target, now) + amount;
        Current = Current with { Target = target };
        IsInFinalCountdown = false;
        ResetWarnings(target - now);
        _generation++;
        _stateStore.Save(Current);
        StateChanged?.Invoke(this, EventArgs.Empty);
        Evaluate();
    }

    /// <summary>
    /// Checks the clock and raises any due events. Runs on every tick; the App also calls it on resume
    /// from sleep so the UI catches up immediately.
    /// </summary>
    public void Evaluate()
    {
        if (Current is null)
            return;

        var generation = _generation;
        var now = _clock.Now;
        var remaining = Current.Target - now;

        if (!IsInFinalCountdown && remaining < FinalCountdownDuration - LateTolerance)
        {
            // Missed the start of the final countdown: give the user the full countdown anyway.
            Current = Current with { Target = now + FinalCountdownDuration };
            remaining = FinalCountdownDuration;
            _stateStore.Save(Current);
            StateChanged?.Invoke(this, EventArgs.Empty);
            if (generation != _generation)
                return;
        }

        RaiseWarnings(remaining);
        if (generation != _generation)
            return;

        if (!IsInFinalCountdown && remaining <= FinalCountdownDuration)
        {
            IsInFinalCountdown = true;
            FinalCountdownStarted?.Invoke(this, Current.Target);
            if (generation != _generation)
                return;
        }

        Tick?.Invoke(this, Max(remaining, TimeSpan.Zero));
        if (generation != _generation)
            return;

        if (IsInFinalCountdown && remaining <= TimeSpan.Zero)
            ExecuteShutdown();
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void Activate(ScheduleState state)
    {
        Current = state;
        IsInFinalCountdown = false;
        ResetWarnings(state.Target - _clock.Now);
        _generation++;
        _stateStore.Save(state);
        _timer ??= _timerFactory.Start(TickInterval, Evaluate);
        StateChanged?.Invoke(this, EventArgs.Empty);
        Evaluate();
    }

    private void Deactivate()
    {
        _timer?.Dispose();
        _timer = null;
        Current = null;
        IsInFinalCountdown = false;
        _passedWarnings.Clear();
        _generation++;
        _stateStore.Clear();
    }

    private void ExecuteShutdown()
    {
        var options = Current!.Options;
        Deactivate();
        StateChanged?.Invoke(this, EventArgs.Empty);
        DueReached?.Invoke(this, options);

        try
        {
            _executor.Execute(options);
        }
        catch (Exception ex)
        {
            ShutdownFailed?.Invoke(this, ex);
        }
    }

    /// <summary>Thresholds already at or past <paramref name="remaining"/> are treated as passed, so they never fire late.</summary>
    private void ResetWarnings(TimeSpan remaining)
    {
        _passedWarnings.Clear();
        foreach (var threshold in WarningThresholds)
        {
            if (threshold >= remaining)
                _passedWarnings.Add(threshold);
        }
    }

    /// <summary>Raises only the smallest newly crossed threshold, so a long sleep doesn't produce a burst of stale warnings.</summary>
    private void RaiseWarnings(TimeSpan remaining)
    {
        TimeSpan? crossed = null;
        foreach (var threshold in WarningThresholds)
        {
            if (remaining <= threshold && _passedWarnings.Add(threshold))
                crossed = crossed is null || threshold < crossed ? threshold : crossed;
        }

        if (crossed is { } warning)
            WarningReached?.Invoke(this, warning);
    }

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;

    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;
}

using FluentAssertions;
using ScheduleShutdown.Core.Models;
using ScheduleShutdown.Core.Scheduling;
using ScheduleShutdown.Core.Tests.Fakes;

namespace ScheduleShutdown.Core.Tests;

public class ShutdownSchedulerTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly ShutdownOptions Force = new(Force: true);

    private readonly FakeClock _clock = new(Start);
    private readonly FakeTimerFactory _timers = new();
    private readonly FakeShutdownExecutor _executor = new();
    private readonly InMemoryStateStore _store = new();
    private readonly ShutdownScheduler _scheduler;

    private readonly List<TimeSpan> _ticks = [];
    private readonly List<TimeSpan> _warnings = [];
    private readonly List<DateTimeOffset> _finalCountdowns = [];
    private readonly List<ShutdownOptions> _dues = [];
    private readonly List<Exception> _failures = [];
    private int _stateChanges;

    public ShutdownSchedulerTests()
    {
        _scheduler = new ShutdownScheduler(_clock, _timers, _executor, _store);
        _scheduler.Tick += (_, remaining) => _ticks.Add(remaining);
        _scheduler.WarningReached += (_, threshold) => _warnings.Add(threshold);
        _scheduler.FinalCountdownStarted += (_, target) => _finalCountdowns.Add(target);
        _scheduler.DueReached += (_, options) => _dues.Add(options);
        _scheduler.ShutdownFailed += (_, ex) => _failures.Add(ex);
        _scheduler.StateChanged += (_, _) => _stateChanges++;
    }

    /// <summary>Moves the clock forward one second at a time, firing the timer each second.</summary>
    private void Advance(TimeSpan duration)
    {
        for (var elapsed = TimeSpan.Zero; elapsed < duration; elapsed += ShutdownScheduler.TickInterval)
        {
            _clock.Now += ShutdownScheduler.TickInterval;
            _timers.FireAll();
        }
    }

    /// <summary>Jumps the clock without ticking (sleep/hibernate), then fires the timer once (resume).</summary>
    private void SleepAndResume(TimeSpan duration)
    {
        _clock.Now += duration;
        _timers.FireAll();
    }

    private ScheduleResult ScheduleTimer(TimeSpan duration, ShutdownOptions? options = null) =>
        _scheduler.Schedule(ScheduleRequest.Timer(duration, options ?? ShutdownOptions.Default));

    // ---- Scheduling ----

    [Fact]
    public void Schedule_Timer_ActivatesSavesAndStartsTimer()
    {
        var result = ScheduleTimer(TimeSpan.FromMinutes(30), Force);

        result.IsSuccess.Should().BeTrue();
        _scheduler.IsActive.Should().BeTrue();
        _scheduler.Current.Should().Be(new ScheduleState(Start.AddMinutes(30), ScheduleMode.Timer, Force));
        _scheduler.Remaining.Should().Be(TimeSpan.FromMinutes(30));
        _store.Saved.Should().Be(_scheduler.Current);
        _timers.RunningCount.Should().Be(1);
        _stateChanges.Should().Be(1);
        _ticks.Should().Equal(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void Schedule_AtTime_UsesClockTimeZone()
    {
        var result = _scheduler.Schedule(
            ScheduleRequest.AtTime(new DateOnly(2026, 10, 30), new TimeOnly(13, 0), ShutdownOptions.Default));

        result.IsSuccess.Should().BeTrue();
        _scheduler.Current!.Target.Should().Be(Start.AddHours(1));
        _scheduler.Current.Mode.Should().Be(ScheduleMode.AtTime);
    }

    [Fact]
    public void Schedule_InvalidRequest_ReturnsErrorAndStaysInactive()
    {
        var result = ScheduleTimer(TimeSpan.FromSeconds(10));

        result.Error.Should().Be(ScheduleError.DurationTooShort);
        _scheduler.IsActive.Should().BeFalse();
        _store.Saved.Should().BeNull();
        _timers.RunningCount.Should().Be(0);
        _stateChanges.Should().Be(0);
    }

    [Fact]
    public void Schedule_WhileActive_ReplacesScheduleWithSingleTimer()
    {
        ScheduleTimer(TimeSpan.FromMinutes(30));
        ScheduleTimer(TimeSpan.FromMinutes(5), Force);

        _scheduler.Current!.Target.Should().Be(Start.AddMinutes(5));
        _scheduler.Current.Options.Should().Be(Force);
        _timers.RunningCount.Should().Be(1);

        Advance(TimeSpan.FromMinutes(5));
        _executor.Executions.Should().ContainSingle().Which.Should().Be(Force);
    }

    // ---- Ticking and warnings ----

    [Fact]
    public void Tick_ReportsRemainingEachSecond()
    {
        ScheduleTimer(TimeSpan.FromMinutes(5));

        Advance(TimeSpan.FromSeconds(3));

        _ticks.Should().Equal(
            TimeSpan.FromSeconds(300), TimeSpan.FromSeconds(299), TimeSpan.FromSeconds(298), TimeSpan.FromSeconds(297));
    }

    [Fact]
    public void Warnings_FireOnceAtEachThreshold()
    {
        ScheduleTimer(TimeSpan.FromMinutes(15));

        Advance(TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds(1));
        _warnings.Should().BeEmpty();

        Advance(TimeSpan.FromSeconds(1));
        _warnings.Should().Equal(TimeSpan.FromMinutes(10));
        _scheduler.Remaining.Should().Be(TimeSpan.FromMinutes(10));

        Advance(TimeSpan.FromMinutes(9));
        _warnings.Should().Equal(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1));

        Advance(TimeSpan.FromSeconds(30));
        _warnings.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    public void Warnings_ThresholdsAtOrAboveInitialRemaining_DoNotFire(int minutes)
    {
        ScheduleTimer(TimeSpan.FromMinutes(minutes));

        Advance(TimeSpan.FromMinutes(minutes) - TimeSpan.FromSeconds(1));

        _warnings.Should().Equal(TimeSpan.FromMinutes(1));
    }

    // ---- Final countdown and shutdown ----

    [Fact]
    public void FinalCountdown_StartsAt60Seconds()
    {
        ScheduleTimer(TimeSpan.FromMinutes(5));

        Advance(TimeSpan.FromMinutes(4) - TimeSpan.FromSeconds(1));
        _scheduler.IsInFinalCountdown.Should().BeFalse();
        _finalCountdowns.Should().BeEmpty();

        Advance(TimeSpan.FromSeconds(1));
        _scheduler.IsInFinalCountdown.Should().BeTrue();
        _finalCountdowns.Should().Equal(Start.AddMinutes(5));
    }

    [Fact]
    public void Due_ExecutesShutdownAndClearsSchedule()
    {
        ScheduleTimer(TimeSpan.FromMinutes(5), Force);

        Advance(TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds(1));
        _executor.Executions.Should().BeEmpty();

        Advance(TimeSpan.FromSeconds(1));
        _executor.Executions.Should().Equal(Force);
        _dues.Should().Equal(Force);
        _ticks.Last().Should().Be(TimeSpan.Zero);
        _scheduler.IsActive.Should().BeFalse();
        _scheduler.IsInFinalCountdown.Should().BeFalse();
        _store.Saved.Should().BeNull();
        _timers.RunningCount.Should().Be(0);
    }

    [Fact]
    public void Due_PassesNormalOptionsWhenForceIsOff()
    {
        ScheduleTimer(TimeSpan.FromMinutes(1));

        Advance(TimeSpan.FromMinutes(1));

        _executor.Executions.Should().ContainSingle().Which.Force.Should().BeFalse();
    }

    [Fact]
    public void Due_ExecutorThrows_RaisesShutdownFailed()
    {
        var error = new InvalidOperationException("shutdown.exe missing");
        _executor.ThrowOnExecute = error;
        ScheduleTimer(TimeSpan.FromMinutes(1));

        Advance(TimeSpan.FromMinutes(1));

        _failures.Should().Equal(error);
        _scheduler.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Schedule_OneMinuteTimer_StartsFinalCountdownImmediatelyWithoutMovingTarget()
    {
        ScheduleTimer(TimeSpan.FromMinutes(1));

        _scheduler.IsInFinalCountdown.Should().BeTrue();
        _scheduler.Current!.Target.Should().Be(Start.AddMinutes(1));
        _warnings.Should().BeEmpty();
    }

    [Fact]
    public void Schedule_AtTimeSecondsAway_GivesFullFinalCountdown()
    {
        _clock.Now = Start.AddSeconds(-20);

        _scheduler.Schedule(ScheduleRequest.AtTime(new DateOnly(2026, 10, 30), new TimeOnly(12, 0), ShutdownOptions.Default));

        _scheduler.IsInFinalCountdown.Should().BeTrue();
        _scheduler.Current!.Target.Should().Be(_clock.Now + ShutdownScheduler.FinalCountdownDuration);
        _store.Saved!.Target.Should().Be(_scheduler.Current.Target);
    }

    // ---- Cancel ----

    [Fact]
    public void Cancel_StopsEverything()
    {
        ScheduleTimer(TimeSpan.FromMinutes(5));

        _scheduler.Cancel().Should().BeTrue();

        _scheduler.IsActive.Should().BeFalse();
        _scheduler.Remaining.Should().Be(TimeSpan.Zero);
        _store.Saved.Should().BeNull();
        _timers.RunningCount.Should().Be(0);
        _stateChanges.Should().Be(2);

        Advance(TimeSpan.FromMinutes(10));
        _executor.Executions.Should().BeEmpty();
    }

    [Fact]
    public void Cancel_DuringFinalCountdown_PreventsShutdown()
    {
        ScheduleTimer(TimeSpan.FromMinutes(5));
        Advance(TimeSpan.FromMinutes(4) + TimeSpan.FromSeconds(30));

        _scheduler.Cancel();
        Advance(TimeSpan.FromMinutes(1));

        _executor.Executions.Should().BeEmpty();
    }

    [Fact]
    public void Cancel_WhenInactive_ReturnsFalse()
    {
        _scheduler.Cancel().Should().BeFalse();
        _stateChanges.Should().Be(0);
    }

    [Fact]
    public void Cancel_FromFinalCountdownHandler_PreventsShutdown()
    {
        _scheduler.FinalCountdownStarted += (_, _) => _scheduler.Cancel();
        ScheduleTimer(TimeSpan.FromMinutes(5));

        Advance(TimeSpan.FromMinutes(10));

        _executor.Executions.Should().BeEmpty();
        _scheduler.IsActive.Should().BeFalse();
    }

    // ---- Postpone ----

    [Fact]
    public void Postpone_DuringFinalCountdown_MovesTargetAndLeavesCountdown()
    {
        ScheduleTimer(TimeSpan.FromMinutes(5));
        Advance(TimeSpan.FromMinutes(4) + TimeSpan.FromSeconds(30));

        _scheduler.Postpone(TimeSpan.FromMinutes(10));

        _scheduler.Current!.Target.Should().Be(Start.AddMinutes(15));
        _scheduler.Remaining.Should().Be(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(30));
        _scheduler.IsInFinalCountdown.Should().BeFalse();
        _store.Saved!.Target.Should().Be(Start.AddMinutes(15));
        _executor.Executions.Should().BeEmpty();
    }

    [Fact]
    public void Postpone_RearmsWarningsAndFinalCountdown()
    {
        ScheduleTimer(TimeSpan.FromMinutes(5));
        Advance(TimeSpan.FromMinutes(4) + TimeSpan.FromSeconds(30));
        _warnings.Clear();
        _finalCountdowns.Clear();

        _scheduler.Postpone(TimeSpan.FromMinutes(10));
        Advance(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(30));

        _warnings.Should().Equal(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1));
        _finalCountdowns.Should().Equal(Start.AddMinutes(15));
        _executor.Executions.Should().ContainSingle();
    }

    [Fact]
    public void Postpone_WhenInactive_Throws()
    {
        var act = () => _scheduler.Postpone(TimeSpan.FromMinutes(10));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Postpone_NonPositiveAmount_Throws()
    {
        ScheduleTimer(TimeSpan.FromMinutes(5));

        var act = () => _scheduler.Postpone(TimeSpan.Zero);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---- Sleep / resume ----

    [Fact]
    public void ResumeAfterTargetPassed_DoesNotShutDownInstantly()
    {
        ScheduleTimer(TimeSpan.FromMinutes(30), Force);

        SleepAndResume(TimeSpan.FromHours(2));

        _executor.Executions.Should().BeEmpty();
        _scheduler.IsInFinalCountdown.Should().BeTrue();
        _scheduler.Current!.Target.Should().Be(_clock.Now + ShutdownScheduler.FinalCountdownDuration);
        _finalCountdowns.Should().Equal(_scheduler.Current.Target);
        _store.Saved!.Target.Should().Be(_scheduler.Current.Target);
    }

    [Fact]
    public void ResumeAfterTargetPassed_ShutsDownAfterFullCountdown()
    {
        ScheduleTimer(TimeSpan.FromMinutes(30), Force);
        SleepAndResume(TimeSpan.FromHours(2));

        Advance(ShutdownScheduler.FinalCountdownDuration - TimeSpan.FromSeconds(1));
        _executor.Executions.Should().BeEmpty();

        Advance(TimeSpan.FromSeconds(1));
        _executor.Executions.Should().Equal(Force);
    }

    [Fact]
    public void ResumeSecondsBeforeTarget_GivesFullCountdown()
    {
        ScheduleTimer(TimeSpan.FromMinutes(30));

        SleepAndResume(TimeSpan.FromMinutes(30) - TimeSpan.FromSeconds(10));

        _scheduler.Remaining.Should().Be(ShutdownScheduler.FinalCountdownDuration);
        _scheduler.IsInFinalCountdown.Should().BeTrue();
    }

    [Fact]
    public void ResumeAcrossSeveralThresholds_RaisesOnlyTheSmallestWarning()
    {
        ScheduleTimer(TimeSpan.FromMinutes(30));

        SleepAndResume(TimeSpan.FromMinutes(29) + TimeSpan.FromSeconds(30));

        _warnings.Should().Equal(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void ResumeBeforeAnyThreshold_ContinuesNormally()
    {
        ScheduleTimer(TimeSpan.FromMinutes(30));

        SleepAndResume(TimeSpan.FromMinutes(10));

        _scheduler.Current!.Target.Should().Be(Start.AddMinutes(30));
        _scheduler.IsInFinalCountdown.Should().BeFalse();
        _warnings.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_CanBeCalledDirectlyOnResume()
    {
        ScheduleTimer(TimeSpan.FromMinutes(30));
        _clock.Now += TimeSpan.FromMinutes(25);

        _scheduler.Evaluate();

        _ticks.Last().Should().Be(TimeSpan.FromMinutes(5));
        _warnings.Should().Equal(TimeSpan.FromMinutes(10));
    }

    // ---- Restore ----

    [Fact]
    public void Restore_NothingSaved()
    {
        _scheduler.Restore().Should().Be(RestoreOutcome.NothingSaved);

        _scheduler.IsActive.Should().BeFalse();
        _timers.RunningCount.Should().Be(0);
    }

    [Fact]
    public void Restore_FutureTarget_ResumesSchedule()
    {
        var saved = new ScheduleState(Start.AddMinutes(45), ScheduleMode.AtTime, Force);
        _store.Saved = saved;

        _scheduler.Restore().Should().Be(RestoreOutcome.Restored);

        _scheduler.Current.Should().Be(saved);
        _scheduler.IsInFinalCountdown.Should().BeFalse();
        _timers.RunningCount.Should().Be(1);
        _ticks.Should().Equal(TimeSpan.FromMinutes(45));

        Advance(TimeSpan.FromMinutes(45));
        _executor.Executions.Should().Equal(Force);
    }

    [Fact]
    public void Restore_OverdueTarget_StartsFinalCountdownInsteadOfShuttingDown()
    {
        _store.Saved = new ScheduleState(Start.AddHours(-1), ScheduleMode.Timer, Force);

        _scheduler.Restore().Should().Be(RestoreOutcome.RestoredOverdue);

        _executor.Executions.Should().BeEmpty();
        _scheduler.IsInFinalCountdown.Should().BeTrue();
        _scheduler.Current!.Target.Should().Be(Start + ShutdownScheduler.FinalCountdownDuration);
        _warnings.Should().BeEmpty();

        Advance(ShutdownScheduler.FinalCountdownDuration);
        _executor.Executions.Should().Equal(Force);
    }

    [Fact]
    public void Restore_ExactlyAtStaleThreshold_IsStillRestored()
    {
        _store.Saved = new ScheduleState(Start - ShutdownScheduler.StaleThreshold, ScheduleMode.Timer, ShutdownOptions.Default);

        _scheduler.Restore().Should().Be(RestoreOutcome.RestoredOverdue);
    }

    [Fact]
    public void Restore_StaleTarget_DiscardsIt()
    {
        _store.Saved = new ScheduleState(
            Start - ShutdownScheduler.StaleThreshold - TimeSpan.FromSeconds(1), ScheduleMode.Timer, Force);

        _scheduler.Restore().Should().Be(RestoreOutcome.DiscardedStale);

        _scheduler.IsActive.Should().BeFalse();
        _store.Saved.Should().BeNull();
        _timers.RunningCount.Should().Be(0);
        _executor.Executions.Should().BeEmpty();
    }

    [Fact]
    public void Restore_WhileActive_Throws()
    {
        ScheduleTimer(TimeSpan.FromMinutes(5));

        var act = () => _scheduler.Restore();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Dispose_StopsTimer()
    {
        ScheduleTimer(TimeSpan.FromMinutes(5));

        _scheduler.Dispose();

        _timers.RunningCount.Should().Be(0);
    }
}

namespace ScheduleShutdown.Core.Models;

/// <summary>What the user asked for. Turned into an absolute target by <see cref="Scheduling.ScheduleCalculator"/>.</summary>
public sealed record ScheduleRequest(
    ScheduleMode Mode,
    DateOnly Date,
    TimeOnly Time,
    TimeSpan Duration,
    ShutdownOptions Options)
{
    public static ScheduleRequest AtTime(DateOnly date, TimeOnly time, ShutdownOptions options) =>
        new(ScheduleMode.AtTime, date, time, TimeSpan.Zero, options);

    public static ScheduleRequest Timer(TimeSpan duration, ShutdownOptions options) =>
        new(ScheduleMode.Timer, default, default, duration, options);
}

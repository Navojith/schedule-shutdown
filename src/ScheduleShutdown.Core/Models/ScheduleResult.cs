namespace ScheduleShutdown.Core.Models;

public enum ScheduleError
{
    TimeInPast,
    TimeTooFarAhead,
    DurationTooShort,
    DurationTooLong,
}

public sealed record ScheduleResult(DateTimeOffset? Target, ScheduleError? Error)
{
    public bool IsSuccess => Error is null;

    public static ScheduleResult Success(DateTimeOffset target) => new(target, null);

    public static ScheduleResult Failure(ScheduleError error) => new(null, error);
}

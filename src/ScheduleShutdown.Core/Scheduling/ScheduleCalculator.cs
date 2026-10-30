using ScheduleShutdown.Core.Models;

namespace ScheduleShutdown.Core.Scheduling;

/// <summary>Turns a <see cref="ScheduleRequest"/> into an absolute target time.</summary>
public static class ScheduleCalculator
{
    public static readonly TimeSpan MinimumDuration = TimeSpan.FromMinutes(1);

    /// <summary>Furthest ahead a shutdown can be scheduled, in either mode.</summary>
    public static readonly TimeSpan MaximumLeadTime = TimeSpan.FromDays(7);

    /// <summary>Today if <paramref name="time"/> is still ahead today, otherwise tomorrow.</summary>
    public static DateOnly SuggestDate(TimeOnly time, DateTimeOffset now, TimeZoneInfo timeZone)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).DateTime);
        return ToTarget(today, time, timeZone) > now ? today : today.AddDays(1);
    }

    /// <summary>Interprets a local wall-clock date and time in <paramref name="timeZone"/>.</summary>
    /// <remarks>
    /// Times skipped by a DST jump use the standard offset (02:30 becomes 03:30 daylight time).
    /// Repeated times resolve to the standard-time occurrence.
    /// </remarks>
    public static DateTimeOffset ToTarget(DateOnly date, TimeOnly time, TimeZoneInfo timeZone)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, timeZone.GetUtcOffset(local));
    }

    public static ScheduleResult Calculate(ScheduleRequest request, DateTimeOffset now, TimeZoneInfo timeZone)
    {
        switch (request.Mode)
        {
            case ScheduleMode.AtTime:
                var target = ToTarget(request.Date, request.Time, timeZone);
                if (target <= now)
                    return ScheduleResult.Failure(ScheduleError.TimeInPast);
                if (target - now > MaximumLeadTime)
                    return ScheduleResult.Failure(ScheduleError.TimeTooFarAhead);
                return ScheduleResult.Success(target);

            case ScheduleMode.Timer:
                if (request.Duration < MinimumDuration)
                    return ScheduleResult.Failure(ScheduleError.DurationTooShort);
                if (request.Duration > MaximumLeadTime)
                    return ScheduleResult.Failure(ScheduleError.DurationTooLong);
                return ScheduleResult.Success(now + request.Duration);

            default:
                throw new ArgumentOutOfRangeException(nameof(request), request.Mode, "Unknown schedule mode.");
        }
    }
}

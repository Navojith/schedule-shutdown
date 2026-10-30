using ScheduleShutdown.Core.Abstractions;

namespace ScheduleShutdown.Core.Scheduling;

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;

    public TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;
}

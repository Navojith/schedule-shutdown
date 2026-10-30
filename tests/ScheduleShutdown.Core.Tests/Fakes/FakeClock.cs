using ScheduleShutdown.Core.Abstractions;

namespace ScheduleShutdown.Core.Tests.Fakes;

public sealed class FakeClock(DateTimeOffset now, TimeZoneInfo? timeZone = null) : IClock
{
    public DateTimeOffset Now { get; set; } = now;

    public TimeZoneInfo LocalTimeZone { get; } = timeZone ?? TimeZoneInfo.Utc;
}

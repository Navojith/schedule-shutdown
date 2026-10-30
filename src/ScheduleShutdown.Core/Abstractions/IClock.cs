namespace ScheduleShutdown.Core.Abstractions;

public interface IClock
{
    DateTimeOffset Now { get; }

    TimeZoneInfo LocalTimeZone { get; }
}

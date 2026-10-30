using System.Globalization;

namespace ScheduleShutdown.Core.Formatting;

public static class ScheduleFormatter
{
    /// <summary>
    /// <c>hh:mm:ss</c>, with hours past 24 (up to the 7-day limit). Rounds up, so it never shows
    /// 00:00:00 before the shutdown is actually due.
    /// </summary>
    public static string FormatRemaining(TimeSpan remaining)
    {
        var ticks = Math.Max(remaining.Ticks, 0);
        var totalSeconds = (ticks + TimeSpan.TicksPerSecond - 1) / TimeSpan.TicksPerSecond;
        return $"{totalSeconds / 3600:00}:{totalSeconds / 60 % 60:00}:{totalSeconds % 60:00}";
    }

    /// <summary>"today at 23:30", "tomorrow at 07:00" or "on Sat 1 Nov at 07:00", in local time.</summary>
    public static string DescribeTarget(DateTimeOffset target, DateTimeOffset now, TimeZoneInfo timeZone, CultureInfo culture)
    {
        var local = TimeZoneInfo.ConvertTime(target, timeZone).DateTime;
        var today = TimeZoneInfo.ConvertTime(now, timeZone).Date;
        var time = local.ToString(culture.DateTimeFormat.ShortTimePattern, culture);

        return (local.Date - today).Days switch
        {
            0 => $"today at {time}",
            1 => $"tomorrow at {time}",
            _ => $"on {local.ToString("ddd d MMM", culture)} at {time}",
        };
    }
}

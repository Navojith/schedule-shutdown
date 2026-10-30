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

    /// <summary>Whole seconds left for the final countdown dialog, rounded up like <see cref="FormatRemaining"/>.</summary>
    public static int CountdownSeconds(TimeSpan remaining)
    {
        var ticks = Math.Max(remaining.Ticks, 0);
        return (int)((ticks + TimeSpan.TicksPerSecond - 1) / TimeSpan.TicksPerSecond);
    }

    /// <summary>"10 minutes", "1 minute" or "30 seconds": how far ahead a warning threshold is.</summary>
    public static string DescribeLeadTime(TimeSpan lead)
    {
        if (lead >= TimeSpan.FromMinutes(1))
        {
            var minutes = (int)Math.Round(lead.TotalMinutes);
            return minutes == 1 ? "1 minute" : $"{minutes} minutes";
        }

        var seconds = CountdownSeconds(lead);
        return seconds == 1 ? "1 second" : $"{seconds} seconds";
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

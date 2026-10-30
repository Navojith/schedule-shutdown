using System.Globalization;
using FluentAssertions;
using ScheduleShutdown.Core.Formatting;

namespace ScheduleShutdown.Core.Tests;

public class ScheduleFormatterTests
{
    private static readonly TimeZoneInfo Colombo =
        TimeZoneInfo.CreateCustomTimeZone("Test+05:30", TimeSpan.FromHours(5.5), "Test+05:30", "Test+05:30");

    private static readonly DateTimeOffset Now = new(2026, 10, 30, 14, 0, 0, TimeSpan.FromHours(5.5));

    [Theory]
    [InlineData(0, "00:00:00")]
    [InlineData(59, "00:00:59")]
    [InlineData(5397, "01:29:57")]
    [InlineData(7 * 24 * 3600, "168:00:00")]
    public void FormatRemaining_WholeSeconds(int seconds, string expected)
    {
        ScheduleFormatter.FormatRemaining(TimeSpan.FromSeconds(seconds)).Should().Be(expected);
    }

    [Fact]
    public void FormatRemaining_RoundsUpPartialSeconds()
    {
        ScheduleFormatter.FormatRemaining(TimeSpan.FromMilliseconds(200)).Should().Be("00:00:01");
        ScheduleFormatter.FormatRemaining(TimeSpan.FromMilliseconds(59_400)).Should().Be("00:01:00");
    }

    [Fact]
    public void FormatRemaining_NegativeIsZero()
    {
        ScheduleFormatter.FormatRemaining(TimeSpan.FromSeconds(-5)).Should().Be("00:00:00");
    }

    [Theory]
    [InlineData(23, 30, 0, "today at 23:30")]
    [InlineData(7, 0, 1, "tomorrow at 07:00")]
    [InlineData(7, 0, 2, "on Sun 1 Nov at 07:00")]
    public void DescribeTarget_RelativeToLocalToday(int hour, int minute, int daysAhead, string expected)
    {
        var target = new DateTimeOffset(2026, 10, 30, hour, minute, 0, TimeSpan.FromHours(5.5)).AddDays(daysAhead);

        ScheduleFormatter.DescribeTarget(target, Now, Colombo, CultureInfo.InvariantCulture).Should().Be(expected);
    }

    [Fact]
    public void DescribeTarget_ConvertsUtcTargetToLocalTime()
    {
        var target = new DateTimeOffset(2026, 10, 30, 18, 0, 0, TimeSpan.Zero);

        ScheduleFormatter.DescribeTarget(target, Now, Colombo, CultureInfo.InvariantCulture)
            .Should().Be("today at 23:30");
    }
}

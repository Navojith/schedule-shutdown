using FluentAssertions;
using ScheduleShutdown.Core.Models;
using ScheduleShutdown.Core.Scheduling;

namespace ScheduleShutdown.Core.Tests;

public class ScheduleCalculatorTests
{
    private static readonly TimeZoneInfo Colombo =
        TimeZoneInfo.CreateCustomTimeZone("Test+05:30", TimeSpan.FromHours(5.5), "Test+05:30", "Test+05:30");

    // 2026-10-30 14:00 local (+05:30)
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 14, 0, 0, TimeSpan.FromHours(5.5));

    [Fact]
    public void SuggestDate_TimeLaterToday_ReturnsToday()
    {
        ScheduleCalculator.SuggestDate(new TimeOnly(23, 30), Now, Colombo)
            .Should().Be(new DateOnly(2026, 10, 30));
    }

    [Theory]
    [InlineData(9, 0)]
    [InlineData(14, 0)]
    public void SuggestDate_TimeAlreadyPassedOrNow_ReturnsTomorrow(int hour, int minute)
    {
        ScheduleCalculator.SuggestDate(new TimeOnly(hour, minute), Now, Colombo)
            .Should().Be(new DateOnly(2026, 10, 31));
    }

    [Fact]
    public void SuggestDate_UsesLocalDateNotUtcDate()
    {
        // 01:00 local on Oct 31 is still Oct 30 in UTC.
        var now = new DateTimeOffset(2026, 10, 31, 1, 0, 0, TimeSpan.FromHours(5.5));

        ScheduleCalculator.SuggestDate(new TimeOnly(2, 0), now, Colombo)
            .Should().Be(new DateOnly(2026, 10, 31));
    }

    [Fact]
    public void ToTarget_UsesTimeZoneOffset()
    {
        ScheduleCalculator.ToTarget(new DateOnly(2026, 10, 30), new TimeOnly(23, 30), Colombo)
            .Should().Be(new DateTimeOffset(2026, 10, 30, 18, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Calculate_AtTimeInFuture_ReturnsTarget()
    {
        var request = ScheduleRequest.AtTime(new DateOnly(2026, 10, 30), new TimeOnly(23, 30), ShutdownOptions.Default);

        var result = ScheduleCalculator.Calculate(request, Now, Colombo);

        result.IsSuccess.Should().BeTrue();
        result.Target.Should().Be(new DateTimeOffset(2026, 10, 30, 23, 30, 0, TimeSpan.FromHours(5.5)));
    }

    [Theory]
    [InlineData(13, 59)]
    [InlineData(14, 0)]
    public void Calculate_AtTimeNotInFuture_FailsWithTimeInPast(int hour, int minute)
    {
        var request = ScheduleRequest.AtTime(new DateOnly(2026, 10, 30), new TimeOnly(hour, minute), ShutdownOptions.Default);

        var result = ScheduleCalculator.Calculate(request, Now, Colombo);

        result.Error.Should().Be(ScheduleError.TimeInPast);
        result.Target.Should().BeNull();
    }

    [Fact]
    public void Calculate_AtTimeBeyondMaximumLeadTime_Fails()
    {
        var request = ScheduleRequest.AtTime(new DateOnly(2026, 11, 6), new TimeOnly(14, 1), ShutdownOptions.Default);

        ScheduleCalculator.Calculate(request, Now, Colombo).Error.Should().Be(ScheduleError.TimeTooFarAhead);
    }

    [Fact]
    public void Calculate_AtTimeExactlyMaximumLeadTime_Succeeds()
    {
        var request = ScheduleRequest.AtTime(new DateOnly(2026, 11, 6), new TimeOnly(14, 0), ShutdownOptions.Default);

        ScheduleCalculator.Calculate(request, Now, Colombo).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Calculate_Timer_ReturnsNowPlusDuration()
    {
        var request = ScheduleRequest.Timer(TimeSpan.FromMinutes(90), ShutdownOptions.Default);

        ScheduleCalculator.Calculate(request, Now, Colombo).Target.Should().Be(Now.AddMinutes(90));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(59)]
    public void Calculate_TimerShorterThanMinimum_Fails(int seconds)
    {
        var request = ScheduleRequest.Timer(TimeSpan.FromSeconds(seconds), ShutdownOptions.Default);

        ScheduleCalculator.Calculate(request, Now, Colombo).Error.Should().Be(ScheduleError.DurationTooShort);
    }

    [Fact]
    public void Calculate_TimerLongerThanMaximum_Fails()
    {
        var request = ScheduleRequest.Timer(TimeSpan.FromDays(7) + TimeSpan.FromMinutes(1), ShutdownOptions.Default);

        ScheduleCalculator.Calculate(request, Now, Colombo).Error.Should().Be(ScheduleError.DurationTooLong);
    }
}

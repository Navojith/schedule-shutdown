using FluentAssertions;
using ScheduleShutdown.Core.Execution;
using ScheduleShutdown.Core.Models;

namespace ScheduleShutdown.Core.Tests;

public class ShutdownExecutionTests
{
    private const string System32 = @"C:\Windows\System32";

    [Fact]
    public void Command_Normal_IsImmediateWithoutForce()
    {
        var command = ShutdownCommand.For(ShutdownOptions.Default, System32);

        command.FileName.Should().Be(@"C:\Windows\System32\shutdown.exe");
        command.Arguments.Should().Equal("/s", "/t", "0");
    }

    [Fact]
    public void Command_Force_AddsForceFlag()
    {
        ShutdownCommand.For(new ShutdownOptions(Force: true), System32)
            .Arguments.Should().Equal("/s", "/t", "0", "/f");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Command_NeverUsesNonZeroDelay(bool force)
    {
        // shutdown /t N with N > 0 implies /f, which would silently force-close apps.
        var arguments = ShutdownCommand.For(new ShutdownOptions(force), System32).Arguments.ToList();

        arguments[arguments.IndexOf("/t") + 1].Should().Be("0");
    }

    [Fact]
    public void DryRun_Normal_LogsCommand()
    {
        var log = new List<string>();

        new DryRunShutdownExecutor(log.Add, System32).Execute(ShutdownOptions.Default);

        log.Should().Equal(@"[DRY RUN] Would run: C:\Windows\System32\shutdown.exe /s /t 0");
    }

    [Fact]
    public void DryRun_Force_LogsCommandWithForceFlag()
    {
        var log = new List<string>();

        new DryRunShutdownExecutor(log.Add, System32).Execute(new ShutdownOptions(Force: true));

        log.Should().Equal(@"[DRY RUN] Would run: C:\Windows\System32\shutdown.exe /s /t 0 /f");
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("true", true)]
    [InlineData(" TRUE ", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("0", false)]
    [InlineData("false", false)]
    [InlineData("yes", false)]
    public void DryRunMode_IsEnabled(string? value, bool expected)
    {
        DryRunMode.IsEnabled(value).Should().Be(expected);
    }
}

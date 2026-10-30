using System.Diagnostics;
using ScheduleShutdown.Core.Abstractions;
using ScheduleShutdown.Core.Execution;
using ScheduleShutdown.Core.Models;

namespace ScheduleShutdown.App.Services;

/// <summary>Runs the real <c>shutdown.exe</c>. Never used in dry-run mode.</summary>
public sealed class ProcessShutdownExecutor(AppLog log) : IShutdownExecutor
{
    private static readonly TimeSpan ExitTimeout = TimeSpan.FromSeconds(10);

    public void Execute(ShutdownOptions options)
    {
        var command = ShutdownCommand.For(options, Environment.SystemDirectory);
        var startInfo = new ProcessStartInfo(command.FileName, command.Arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        log.Write($"Running: {command}");
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start {command.FileName}.");

        // shutdown.exe returns as soon as Windows accepts the request.
        if (!process.WaitForExit(ExitTimeout))
            throw new TimeoutException($"{command.FileName} did not exit within {ExitTimeout.TotalSeconds:0} s.");
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{command.FileName} exited with code {process.ExitCode}.");
    }
}

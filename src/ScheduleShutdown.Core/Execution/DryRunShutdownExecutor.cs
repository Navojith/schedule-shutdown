using ScheduleShutdown.Core.Abstractions;
using ScheduleShutdown.Core.Models;

namespace ScheduleShutdown.Core.Execution;

/// <summary>Logs the shutdown command instead of running it. Used whenever dry-run mode is on.</summary>
public sealed class DryRunShutdownExecutor(Action<string> log, string systemDirectory) : IShutdownExecutor
{
    public void Execute(ShutdownOptions options) =>
        log($"[DRY RUN] Would run: {ShutdownCommand.For(options, systemDirectory)}");
}

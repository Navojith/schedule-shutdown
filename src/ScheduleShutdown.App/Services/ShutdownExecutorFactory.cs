using ScheduleShutdown.Core.Abstractions;
using ScheduleShutdown.Core.Execution;

namespace ScheduleShutdown.App.Services;

public static class ShutdownExecutorFactory
{
    /// <summary>The dry-run executor when <c>SCHEDULE_SHUTDOWN_DRYRUN</c> is set, otherwise the real one.</summary>
    public static IShutdownExecutor Create(AppLog log) =>
        DryRunMode.IsEnabledForCurrentProcess()
            ? new DryRunShutdownExecutor(log.Write, Environment.SystemDirectory)
            : new ProcessShutdownExecutor(log);
}

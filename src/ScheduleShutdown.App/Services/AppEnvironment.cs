namespace ScheduleShutdown.App.Services;

/// <param name="IsDryRun">Shutdown commands are logged instead of run (<c>SCHEDULE_SHUTDOWN_DRYRUN</c>).</param>
public sealed record AppEnvironment(bool IsDryRun);

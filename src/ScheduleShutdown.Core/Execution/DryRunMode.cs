namespace ScheduleShutdown.Core.Execution;

public static class DryRunMode
{
    public const string EnvironmentVariable = "SCHEDULE_SHUTDOWN_DRYRUN";

    /// <summary>True for "1" or "true" (any case, surrounding whitespace ignored).</summary>
    public static bool IsEnabled(string? value) =>
        value?.Trim() is { } trimmed && (trimmed == "1" || trimmed.Equals("true", StringComparison.OrdinalIgnoreCase));

    public static bool IsEnabledForCurrentProcess() =>
        IsEnabled(Environment.GetEnvironmentVariable(EnvironmentVariable));
}

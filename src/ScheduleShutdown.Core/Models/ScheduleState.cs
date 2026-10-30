namespace ScheduleShutdown.Core.Models;

/// <summary>An active schedule. This is what gets persisted.</summary>
/// <param name="Target">Absolute shutdown moment, in both modes, so sleep/hibernate don't skew it.</param>
public sealed record ScheduleState(DateTimeOffset Target, ScheduleMode Mode, ShutdownOptions Options);

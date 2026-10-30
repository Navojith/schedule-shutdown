namespace ScheduleShutdown.Core.Models;

public enum ScheduleMode
{
    /// <summary>Shut down at a specific date and time.</summary>
    AtTime,

    /// <summary>Shut down after a duration.</summary>
    Timer,
}

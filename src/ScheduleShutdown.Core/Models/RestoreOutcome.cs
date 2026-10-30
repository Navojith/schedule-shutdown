namespace ScheduleShutdown.Core.Models;

public enum RestoreOutcome
{
    /// <summary>No saved schedule.</summary>
    NothingSaved,

    /// <summary>Saved schedule is still in the future and is running again.</summary>
    Restored,

    /// <summary>Saved target already passed; the final countdown runs before shutting down.</summary>
    RestoredOverdue,

    /// <summary>Saved target is too old and was discarded.</summary>
    DiscardedStale,
}

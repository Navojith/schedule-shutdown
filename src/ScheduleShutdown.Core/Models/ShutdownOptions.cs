namespace ScheduleShutdown.Core.Models;

/// <param name="Force">Close apps without waiting for them (<c>shutdown /f</c>). Unsaved work is lost.</param>
public sealed record ShutdownOptions(bool Force = false)
{
    public static ShutdownOptions Default { get; } = new();
}

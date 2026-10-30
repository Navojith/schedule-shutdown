namespace ScheduleShutdown.Core.Models;

public enum AppTheme
{
    /// <summary>Follow the Windows light/dark setting.</summary>
    System,
    Light,
    Dark,
}

/// <summary>User preferences, saved separately from the schedule.</summary>
public sealed record AppSettings(AppTheme Theme = AppTheme.System)
{
    public static AppSettings Default { get; } = new();
}

using ScheduleShutdown.Core.Models;

namespace ScheduleShutdown.Core.Abstractions;

public interface IShutdownExecutor
{
    /// <summary>Shuts the PC down now. Throws if the shutdown command could not be started.</summary>
    void Execute(ShutdownOptions options);
}

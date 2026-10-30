using ScheduleShutdown.Core.Models;

namespace ScheduleShutdown.Core.Abstractions;

public interface IStateStore
{
    /// <summary>Returns the saved schedule, or null if there is none or it can't be read.</summary>
    ScheduleState? Load();

    void Save(ScheduleState state);

    void Clear();
}

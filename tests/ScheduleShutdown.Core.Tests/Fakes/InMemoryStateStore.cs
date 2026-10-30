using ScheduleShutdown.Core.Abstractions;
using ScheduleShutdown.Core.Models;

namespace ScheduleShutdown.Core.Tests.Fakes;

public sealed class InMemoryStateStore : IStateStore
{
    public ScheduleState? Saved { get; set; }

    public ScheduleState? Load() => Saved;

    public void Save(ScheduleState state) => Saved = state;

    public void Clear() => Saved = null;
}

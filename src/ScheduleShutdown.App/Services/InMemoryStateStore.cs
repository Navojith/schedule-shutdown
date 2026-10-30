using ScheduleShutdown.Core.Abstractions;
using ScheduleShutdown.Core.Models;

namespace ScheduleShutdown.App.Services;

/// <summary>Placeholder until <c>JsonStateStore</c> lands in Phase 5: the schedule doesn't survive a restart.</summary>
public sealed class InMemoryStateStore : IStateStore
{
    private ScheduleState? _state;

    public ScheduleState? Load() => _state;

    public void Save(ScheduleState state) => _state = state;

    public void Clear() => _state = null;
}

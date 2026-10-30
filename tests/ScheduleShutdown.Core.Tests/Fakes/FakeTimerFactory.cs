using ScheduleShutdown.Core.Abstractions;

namespace ScheduleShutdown.Core.Tests.Fakes;

public sealed class FakeTimerFactory : ITimerFactory
{
    private readonly List<FakeTimer> _timers = [];

    public int RunningCount => _timers.Count(t => !t.IsDisposed);

    public ITickTimer Start(TimeSpan interval, Action onTick)
    {
        var timer = new FakeTimer(interval, onTick);
        _timers.Add(timer);
        return timer;
    }

    /// <summary>Fires every running timer once.</summary>
    public void FireAll()
    {
        foreach (var timer in _timers.Where(t => !t.IsDisposed).ToList())
            timer.OnTick();
    }

    public sealed class FakeTimer(TimeSpan interval, Action onTick) : ITickTimer
    {
        public TimeSpan Interval { get; } = interval;

        public Action OnTick { get; } = onTick;

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }
}

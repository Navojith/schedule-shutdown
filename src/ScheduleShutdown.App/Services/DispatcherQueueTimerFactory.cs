using Microsoft.UI.Dispatching;
using ScheduleShutdown.Core.Abstractions;

namespace ScheduleShutdown.App.Services;

/// <summary>Ticks on the UI thread, which is what <see cref="Core.Scheduling.ShutdownScheduler"/> expects.</summary>
public sealed class DispatcherQueueTimerFactory(DispatcherQueue dispatcherQueue) : ITimerFactory
{
    public ITickTimer Start(TimeSpan interval, Action onTick)
    {
        var timer = dispatcherQueue.CreateTimer();
        timer.Interval = interval;
        timer.IsRepeating = true;
        timer.Tick += (_, _) => onTick();
        timer.Start();
        return new TickTimer(timer);
    }

    private sealed class TickTimer(DispatcherQueueTimer timer) : ITickTimer
    {
        public void Dispose() => timer.Stop();
    }
}

namespace ScheduleShutdown.Core.Abstractions;

public interface ITimerFactory
{
    /// <summary>Creates a started timer that calls <paramref name="onTick"/> every <paramref name="interval"/>.</summary>
    /// <remarks>The App implementation ticks on the UI thread; <see cref="Scheduling.ShutdownScheduler"/> is not thread-safe.</remarks>
    ITickTimer Start(TimeSpan interval, Action onTick);
}

/// <summary>A running timer. Dispose to stop it.</summary>
public interface ITickTimer : IDisposable;

using ScheduleShutdown.Core.Abstractions;
using ScheduleShutdown.Core.Models;

namespace ScheduleShutdown.Core.Tests.Fakes;

public sealed class FakeShutdownExecutor : IShutdownExecutor
{
    public List<ShutdownOptions> Executions { get; } = [];

    public Exception? ThrowOnExecute { get; set; }

    public void Execute(ShutdownOptions options)
    {
        Executions.Add(options);
        if (ThrowOnExecute is not null)
            throw ThrowOnExecute;
    }
}

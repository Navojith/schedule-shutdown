using ScheduleShutdown.Core.Models;

namespace ScheduleShutdown.Core.Execution;

/// <summary>The <c>shutdown.exe</c> invocation for a set of options.</summary>
public sealed record ShutdownCommand(string FileName, IReadOnlyList<string> Arguments)
{
    /// <summary>
    /// Always <c>/s /t 0</c>, plus <c>/f</c> only when force is on. Never use <c>/t N</c> with N &gt; 0:
    /// Windows then implies <c>/f</c> and would silently force-close apps.
    /// </summary>
    /// <param name="systemDirectory">Usually <see cref="Environment.SystemDirectory"/>. A full path avoids PATH lookups.</param>
    public static ShutdownCommand For(ShutdownOptions options, string systemDirectory)
    {
        List<string> arguments = ["/s", "/t", "0"];
        if (options.Force)
            arguments.Add("/f");
        return new ShutdownCommand(Path.Combine(systemDirectory, "shutdown.exe"), arguments);
    }

    public override string ToString() => $"{FileName} {string.Join(' ', Arguments)}";
}

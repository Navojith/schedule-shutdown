using ScheduleShutdown.Core.Abstractions;
using ScheduleShutdown.Core.Models;

namespace ScheduleShutdown.Core.Persistence;

/// <summary>
/// Persists the active schedule as a small JSON file. I/O errors are reported to <paramref name="onError"/>
/// and never thrown: losing persistence must not stop the running schedule.
/// </summary>
public sealed class JsonStateStore(string path, Action<string>? onError = null) : IStateStore
{
    public const int CurrentVersion = 1;

    public string Path { get; } = path;

    public ScheduleState? Load()
    {
        var file = JsonFile.Read<StateFile>(Path, onError);
        if (file is null)
            return null;

        if (file is not { Version: CurrentVersion, Target: { } target, Mode: { } mode } || !Enum.IsDefined(mode))
        {
            onError?.Invoke($"Ignored {Path}: unsupported or incomplete state.");
            return null;
        }

        return new ScheduleState(target, mode, new ShutdownOptions(file.Force));
    }

    public void Save(ScheduleState state) =>
        JsonFile.Write(Path, new StateFile(CurrentVersion, state.Target, state.Mode, state.Options.Force), onError);

    public void Clear() => JsonFile.Delete(Path, onError);

    private sealed record StateFile(int Version, DateTimeOffset? Target, ScheduleMode? Mode, bool Force);
}
